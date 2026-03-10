

import torch
import torch.nn as nn
from torchvision import models, transforms
from PIL import Image
import matplotlib

matplotlib.use('Agg')  # Non-interactive backend
import matplotlib.pyplot as plt
import numpy as np

from gradcam_utils import GradCAM, overlay_heatmap, generate_gradcam_explanation

# Configuration
MODEL_PATH = 'lung_cancer_resnet_v2/models/checkpoints/best_real_model.pth'
TEST_IMAGE_PATH = 'test_lung.jpg'  # ← Put your test lung CT scan here
DEVICE = torch.device('cpu')

CLASS_NAMES = ['no_cancer', 'lung_cancer']


def load_model():
    """Load trained ResNet18 model"""
    print("Loading lung cancer model...")

    # Create architecture
    model = models.resnet18(weights=None)
    num_features = model.fc.in_features
    model.fc = nn.Linear(num_features, 2)

    # Load checkpoint
    checkpoint = torch.load(MODEL_PATH, map_location=DEVICE, weights_only=False)

    # Extract model weights
    if 'model_state_dict' in checkpoint:
        state_dict = checkpoint['model_state_dict']
        print(f"Loaded checkpoint - Epoch: {checkpoint.get('epoch', 'N/A')}, "
              f"Val Acc: {checkpoint.get('val_acc', 'N/A'):.2%}")
    else:
        state_dict = checkpoint

    # Remove 'resnet.' prefix if present
    new_state_dict = {}
    for key, value in state_dict.items():
        if key.startswith('resnet.'):
            new_key = key.replace('resnet.', '')
            new_state_dict[new_key] = value
        else:
            new_state_dict[key] = value

    model.load_state_dict(new_state_dict)
    model.to(DEVICE)
    model.eval()

    print("✓ Model loaded!")
    return model


def load_and_preprocess_image(image_path):
    """Load and preprocess CT scan"""
    print(f"Loading CT scan: {image_path}")

    # Load image
    image = Image.open(image_path).convert('RGB')

    # Define transforms
    transform = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406],
                             std=[0.229, 0.224, 0.225])
    ])

    # Transform
    image_tensor = transform(image).unsqueeze(0)

    print("✓ CT scan preprocessed!")
    return image_tensor, image


def visualize_results(original_image, heatmap, overlay, prediction, confidence, class_name, explanation):
    """Visualize Grad-CAM results"""

    fig, axes = plt.subplots(1, 3, figsize=(15, 5))

    # Original image
    axes[0].imshow(original_image)
    axes[0].set_title('Original CT Scan', fontsize=12)
    axes[0].axis('off')

    # Heatmap
    axes[1].imshow(heatmap, cmap='jet')
    axes[1].set_title(f'Grad-CAM Heatmap\n{explanation["focus_pattern"]} pattern', fontsize=12)
    axes[1].axis('off')

    # Overlay
    axes[2].imshow(overlay)
    axes[2].set_title(f'Overlay\n{class_name}: {confidence:.1%}', fontsize=12)
    axes[2].axis('off')

    plt.suptitle(f'Lung Cancer Detection: {class_name} (Confidence: {confidence:.2%})',
                 fontsize=14, fontweight='bold')

    plt.tight_layout()
    plt.savefig('lung_gradcam_result.png', dpi=150, bbox_inches='tight')
    print("✓ Saved visualization to 'lung_gradcam_result.png'")
    plt.close()

    # Print explanation
    print("\n" + "=" * 60)
    print("GRAD-CAM EXPLANATION - LUNG CANCER DETECTION")
    print("=" * 60)
    print(f"Predicted Class: {class_name}")
    print(f"Confidence: {confidence:.2%}")
    print(f"\nFocus Pattern: {explanation['focus_pattern']}")
    print(f"Description: {explanation['focus_description']}")
    print(f"\nActivation Statistics:")
    print(f"  Mean: {explanation['activation_statistics']['mean']:.4f}")
    print(f"  Max: {explanation['activation_statistics']['max']:.4f}")
    print(f"  Std: {explanation['activation_statistics']['std']:.4f}")
    print(f"  High Activation Ratio: {explanation['activation_statistics']['high_activation_ratio']:.2%}")
    print(f"\nInterpretation: {explanation['interpretation']}")

    if explanation['top_regions']:
        print(f"\nTop Contributing Regions in Lung Tissue:")
        for i, region in enumerate(explanation['top_regions'][:3], 1):
            print(f"  {i}. Position ({region['x']}, {region['y']}), Score: {region['score']:.4f}")


def main():
    """Main test function"""
    print("=" * 60)
    print("GRAD-CAM TEST - LUNG CANCER DETECTION")
    print("=" * 60)

    # Load model
    model = load_model()

    # Initialize Grad-CAM
    gradcam = GradCAM(model, model.layer4)
    print("✓ Grad-CAM initialized on layer4")

    # Load and preprocess image
    image_tensor, original_image = load_and_preprocess_image(TEST_IMAGE_PATH)

    # Make prediction
    print("\nMaking prediction...")
    with torch.no_grad():
        outputs = model(image_tensor)
        probabilities = torch.nn.functional.softmax(outputs, dim=1)
        confidence, predicted = torch.max(probabilities, 1)

    predicted_class = int(predicted.item())
    confidence_value = float(confidence.item())
    class_name = CLASS_NAMES[predicted_class]

    print(f"✓ Prediction: {class_name} ({confidence_value:.2%})")

    # Print all probabilities
    print("\nClass Probabilities:")
    probs = probabilities.squeeze().tolist()
    for i, (name, prob) in enumerate(zip(CLASS_NAMES, probs)):
        print(f"  {name}: {prob:.4f} ({prob * 100:.2f}%)")

    # Generate Grad-CAM
    print("\nGenerating Grad-CAM heatmap...")
    heatmap = gradcam.generate_heatmap(image_tensor, target_class=predicted_class)
    print(f"✓ Heatmap shape: {heatmap.shape}")

    # Create overlay
    print("Creating overlay...")
    overlay = overlay_heatmap(original_image, heatmap, alpha=0.4)
    print("✓ Overlay created!")

    # Generate explanation
    explanation = generate_gradcam_explanation(heatmap, predicted_class, confidence_value, class_name)

    # Visualize
    visualize_results(original_image, heatmap, overlay, predicted_class,
                      confidence_value, class_name, explanation)

    # Cleanup
    gradcam.remove_hooks()

    print("\n" + "=" * 60)
    print("TEST COMPLETED SUCCESSFULLY!")
    print("Check 'lung_gradcam_result.png' for visualization")
    print("=" * 60)


if __name__ == '__main__':
    main()