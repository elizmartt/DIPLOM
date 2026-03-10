"""
Grad-CAM Test Script - Simple Version
Tests basic Grad-CAM that saves to shared folder
No enhancements, just clean simple visualization
"""

import torch
import torch.nn as nn
from torchvision import models, transforms
from PIL import Image
import numpy as np
import os

# Set matplotlib to non-interactive backend
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

# Import simple functions
from gradcam_utils import (
    GradCAM,
    overlay_heatmap,
    generate_gradcam_explanation
)

# ═══════════════════════════════════════════════════════════════════════
# CONFIGURATION
# ═══════════════════════════════════════════════════════════════════════

MODEL_PATH = 'neurological_resnet18_best.pth'
IMAGE_PATH = 'test_image.jpg'  # ← CHANGE THIS TO YOUR IMAGE PATH

CLASS_NAMES = [
    'alzheimer_mild', 'alzheimer_moderate', 'alzheimer_very_mild',
    'glioma', 'meningioma', 'multiple_sclerosis', 'normal', 'pituitary'
]

# ═══════════════════════════════════════════════════════════════════════


def load_model(model_path, num_classes=8):
    """Load ResNet18 model"""
    print(f"Loading model from {model_path}...")
    model = models.resnet18(pretrained=False)
    model.fc = nn.Linear(model.fc.in_features, num_classes)
    state_dict = torch.load(model_path, map_location='cpu', weights_only=False)
    model.load_state_dict(state_dict)
    model.eval()
    print("✓ Model loaded")
    return model


def preprocess_image(image_path):
    """Load and preprocess image"""
    print(f"Loading image from {image_path}...")

    if not os.path.exists(image_path):
        raise FileNotFoundError(f"❌ Image not found: {image_path}")

    image = Image.open(image_path).convert('RGB')

    transform = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
    ])

    image_tensor = transform(image).unsqueeze(0)
    print(f"✓ Image loaded: {image.size}")
    return image_tensor, image


def test_simple_gradcam():
    """Test simple Grad-CAM (no enhancements)"""

    print("=" * 80)
    print("SIMPLE GRAD-CAM TEST")
    print("Basic visualization that saves to shared folder")
    print("=" * 80)
    print(f"\n📁 Image: {IMAGE_PATH}")

    # Load model
    print("\n[1/5] Loading model...")
    model = load_model(MODEL_PATH, num_classes=len(CLASS_NAMES))

    # Load image
    print("\n[2/5] Loading image...")
    try:
        image_tensor, original_image = preprocess_image(IMAGE_PATH)
    except FileNotFoundError as e:
        print(e)
        print("\n💡 Update IMAGE_PATH at the top of this script")
        return

    # Get prediction
    print("\n[3/5] Running inference...")
    with torch.no_grad():
        outputs = model(image_tensor)
        probabilities = torch.nn.functional.softmax(outputs, dim=1)
        confidence, predicted = torch.max(probabilities, 1)

    predicted_class = int(predicted.item())
    confidence_value = float(confidence.item())
    class_name = CLASS_NAMES[predicted_class]

    print(f"✓ Prediction: {class_name} ({confidence_value:.1%})")

    # Top 3 predictions
    print(f"\n  Top-3 predictions:")
    top3_probs, top3_indices = torch.topk(probabilities, 3)
    for i, (prob, idx) in enumerate(zip(top3_probs[0], top3_indices[0]), 1):
        print(f"    {i}. {CLASS_NAMES[idx]}: {prob:.2%}")

    # Generate Grad-CAM
    print("\n[4/5] Generating simple Grad-CAM...")

    gradcam = GradCAM(model, model.layer4)

    # Generate heatmap
    print("   - Generating heatmap...")
    heatmap = gradcam.generate_heatmap(image_tensor, predicted_class)

    # Create overlay
    print("   - Creating overlay...")
    overlay = overlay_heatmap(original_image, heatmap, alpha=0.4)

    # Generate explanation
    print("   - Generating explanation...")
    explanation = generate_gradcam_explanation(
        heatmap,
        predicted_class,
        confidence_value,
        class_name
    )

    gradcam.remove_hooks()
    print("✓ Simple Grad-CAM generated")

    # Print analysis
    print("\n" + "=" * 80)
    print("GRAD-CAM ANALYSIS")
    print("=" * 80)

    print(f"\n🏥 Diagnosis: {class_name}")
    print(f"📊 Confidence: {confidence_value:.1%}")
    print(f"🎯 Focus Pattern: {explanation['focus_pattern']}")
    print(f"   → {explanation['focus_description']}")

    stats = explanation['activation_statistics']
    print(f"\n📈 Activation Statistics:")
    print(f"   Maximum: {stats['max']:.3f}")
    print(f"   Mean: {stats['mean']:.3f}")
    print(f"   Std Dev: {stats['std']:.3f}")
    print(f"   High Activation Ratio: {stats['high_activation_ratio']:.1%}")

    print(f"\n💬 Interpretation:")
    print(f"   {explanation['interpretation']}")

    # Save visualizations
    print("\n[5/5] Saving visualizations...")

    output_dir = os.path.dirname(IMAGE_PATH) or '.'
    output_name = os.path.splitext(os.path.basename(IMAGE_PATH))[0]

    # Save overlay (main result)
    overlay_path = os.path.join(output_dir, f"{output_name}_gradcam_overlay.png")
    Image.fromarray(overlay).save(overlay_path)
    print(f"✓ Saved: {output_name}_gradcam_overlay.png")

    # Save raw heatmap
    heatmap_uint8 = (heatmap * 255).astype(np.uint8)
    heatmap_path = os.path.join(output_dir, f"{output_name}_gradcam_heatmap.png")
    Image.fromarray(np.stack([heatmap_uint8]*3, axis=-1)).save(heatmap_path)
    print(f"✓ Saved: {output_name}_gradcam_heatmap.png")

    # Create simple comparison figure
    print("\nGenerating visualization...")

    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    fig.suptitle(f'Grad-CAM: {class_name} ({confidence_value:.1%})',
                 fontsize=16, fontweight='bold')

    # Left: Original image
    axes[0].imshow(original_image)
    axes[0].set_title('Original Image', fontsize=12, fontweight='bold')
    axes[0].axis('off')

    # Right: Overlay
    axes[1].imshow(overlay)
    axes[1].set_title('Grad-CAM Overlay', fontsize=12, fontweight='bold')
    axes[1].axis('off')

    plt.tight_layout()

    comparison_path = os.path.join(output_dir, f"{output_name}_gradcam_comparison.png")
    plt.savefig(comparison_path, dpi=150, bbox_inches='tight')
    plt.close(fig)

    print(f"✓ Comparison saved: {output_name}_gradcam_comparison.png")

    # Summary
    print("\n" + "=" * 80)
    print("TEST COMPLETE! ✅")
    print("=" * 80)
    print("✅ Simple Grad-CAM generated")
    print("✅ No enhancements (basic visualization)")
    print("✅ Saves to shared folder")

    print("\n📂 View saved files:")
    print(f"   {os.path.abspath(output_dir)}")

    print("\n📋 Generated files:")
    print(f"   1. {output_name}_gradcam_overlay.png ⭐ (Main result)")
    print(f"   2. {output_name}_gradcam_heatmap.png (Raw heatmap)")
    print(f"   3. {output_name}_gradcam_comparison.png (Full visualization)")

    print("\n💡 This is the SIMPLE version:")
    print("   • No post-processing")
    print("   • No enhancements")
    print("   • Just clean, basic Grad-CAM")
    print("   • Compatible with shared folder setup")
    print("=" * 80)


if __name__ == '__main__':
    try:
        print(f"\n📂 Current directory: {os.getcwd()}")
        test_simple_gradcam()
    except Exception as e:
        print(f"\n❌ Error: {e}")
        import traceback
        traceback.print_exc()
        print("\n💡 Make sure:")
        print("   1. IMAGE_PATH points to your image")
        print("   2. MODEL_PATH points to your model")
        print("   3. gradcam_utils.py is the SIMPLE version (no enhancements)")