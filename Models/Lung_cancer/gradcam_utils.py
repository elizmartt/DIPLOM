"""
Grad-CAM Implementation for Brain Tumor Visualization
Generates class activation heatmaps for explainable AI
"""

import torch
import torch.nn.functional as F
import numpy as np
import cv2
from PIL import Image


class GradCAM:
    """Grad-CAM implementation for CNN visualization"""

    def __init__(self, model, target_layer):
        """
        Args:
            model: PyTorch model
            target_layer: Target layer for Grad-CAM (e.g., model.layer4)
        """
        self.model = model
        self.target_layer = target_layer
        self.gradients = None
        self.activations = None

        # Register hooks
        self.forward_hook = target_layer.register_forward_hook(self._forward_hook)
        self.backward_hook = target_layer.register_full_backward_hook(self._backward_hook)

    def _forward_hook(self, module, input, output):
        """Save forward activations"""
        self.activations = output.detach()

    def _backward_hook(self, module, grad_input, grad_output):
        """Save backward gradients"""
        self.gradients = grad_output[0].detach()

    def generate_heatmap(self, input_tensor, target_class=None):
        """
        Generate Grad-CAM heatmap

        Args:
            input_tensor: Input image tensor (1, C, H, W)
            target_class: Target class index (if None, uses predicted class)

        Returns:
            heatmap: Numpy array (H, W) with values [0, 1]
        """
        # Forward pass
        self.model.eval()
        output = self.model(input_tensor)

        # Get target class
        if target_class is None:
            target_class = output.argmax(dim=1).item()

        # Zero gradients
        self.model.zero_grad()

        # Backward pass for target class
        class_score = output[0, target_class]
        class_score.backward()

        # Get gradients and activations
        gradients = self.gradients  # (1, C, H', W')
        activations = self.activations  # (1, C, H', W')

        # Global average pooling on gradients
        weights = gradients.mean(dim=(2, 3), keepdim=True)  # (1, C, 1, 1)

        # Weighted combination of activation maps
        cam = (weights * activations).sum(dim=1, keepdim=True)  # (1, 1, H', W')

        # Apply ReLU (only positive contributions)
        cam = F.relu(cam)

        # Normalize to [0, 1]
        cam = cam.squeeze().cpu().numpy()
        cam = cam - cam.min()
        if cam.max() > 0:
            cam = cam / cam.max()

        return cam

    def remove_hooks(self):
        """Remove hooks from model"""
        self.forward_hook.remove()
        self.backward_hook.remove()


def apply_colormap(heatmap, colormap=cv2.COLORMAP_JET):
    """
    Apply colormap to heatmap

    Args:
        heatmap: Numpy array (H, W) with values [0, 1]
        colormap: OpenCV colormap

    Returns:
        Colored heatmap (H, W, 3) with values [0, 255]
    """
    heatmap_uint8 = (heatmap * 255).astype(np.uint8)
    colored_heatmap = cv2.applyColorMap(heatmap_uint8, colormap)
    colored_heatmap = cv2.cvtColor(colored_heatmap, cv2.COLOR_BGR2RGB)
    return colored_heatmap


def overlay_heatmap(image, heatmap, alpha=0.4, x_offset=0, y_offset=0):
    """
    Overlay heatmap on original image

    Args:
        image: PIL Image or numpy array (H, W, 3)
        heatmap: Numpy array (H, W) with values [0, 1]
        alpha: Transparency of heatmap overlay
        x_offset: Horizontal offset in pixels (negative = left, positive = right)
        y_offset: Vertical offset in pixels (negative = up, positive = down)

    Returns:
        overlay: Numpy array (H, W, 3) with overlay
    """
    # Convert PIL Image to numpy if needed
    if isinstance(image, Image.Image):
        image = np.array(image)

    # Ensure image is RGB
    if len(image.shape) == 2:
        image = cv2.cvtColor(image, cv2.COLOR_GRAY2RGB)

    # Resize heatmap to match image size
    h, w = image.shape[:2]
    heatmap_resized = cv2.resize(heatmap, (w, h))

    # Apply colormap
    colored_heatmap = apply_colormap(heatmap_resized)

    # Apply offset if specified
    if x_offset != 0 or y_offset != 0:
        # Create translation matrix
        M = np.float32([[1, 0, x_offset], [0, 1, y_offset]])
        # Shift the heatmap
        colored_heatmap = cv2.warpAffine(colored_heatmap, M, (w, h))

    # Blend images
    overlay = (colored_heatmap * alpha + image * (1 - alpha)).astype(np.uint8)

    return overlay


def get_top_regions(heatmap, threshold=0.7, top_k=5):
    """
    Extract top contributing regions from heatmap

    Args:
        heatmap: Numpy array (H, W) with values [0, 1]
        threshold: Threshold for considering a region important
        top_k: Number of top regions to return

    Returns:
        regions: List of (x, y, score) tuples
    """
    # Find high activation regions
    high_activation_mask = heatmap > threshold

    # Get coordinates and scores
    y_coords, x_coords = np.where(high_activation_mask)

    if len(y_coords) == 0:
        return []

    scores = heatmap[y_coords, x_coords]

    # Sort by score and get top_k
    top_indices = np.argsort(scores)[-top_k:]

    regions = [
        {
            'x': int(x_coords[i]),
            'y': int(y_coords[i]),
            'score': float(scores[i])
        }
        for i in top_indices
    ]

    return regions


def generate_gradcam_explanation(heatmap, predicted_class, confidence, class_name):
    """
    Generate human-readable explanation from Grad-CAM

    Args:
        heatmap: Numpy array (H, W)
        predicted_class: Predicted class index
        confidence: Prediction confidence
        class_name: Name of predicted class

    Returns:
        explanation: Dictionary with explanation data
    """
    # Calculate statistics
    activation_mean = float(heatmap.mean())
    activation_max = float(heatmap.max())
    activation_std = float(heatmap.std())

    # Find focused regions
    high_activation_ratio = float((heatmap > 0.7).sum() / heatmap.size)

    # Determine focus pattern
    if high_activation_ratio > 0.3:
        focus_pattern = "diffuse"
        focus_description = "Multiple regions show high activation"
    elif high_activation_ratio > 0.1:
        focus_pattern = "moderate"
        focus_description = "Several specific regions are highlighted"
    else:
        focus_pattern = "localized"
        focus_description = "Highly focused on specific areas"

    # Get top contributing regions
    top_regions = get_top_regions(heatmap)

    explanation = {
        'predicted_class': class_name,
        'confidence': confidence,
        'activation_statistics': {
            'mean': activation_mean,
            'max': activation_max,
            'std': activation_std,
            'high_activation_ratio': high_activation_ratio
        },
        'focus_pattern': focus_pattern,
        'focus_description': focus_description,
        'top_regions': top_regions,
        'interpretation': f"The model focused {focus_pattern}ly on specific brain regions to identify {class_name} with {confidence:.1%} confidence."
    }

    return explanation