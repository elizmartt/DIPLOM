
import torch
from pathlib import Path

checkpoint = torch.load('models/checkpoints/best_real_model.pth', weights_only=False)

print("=" * 50)
print("TRAINING RESULTS SUMMARY")
print("=" * 50)

print(f"\nBest Model Saved At:")
print(f"  Epoch: {checkpoint['epoch'] + 1}")
print(f"  Validation Accuracy: {checkpoint['val_acc']:.2f}%")
print(f"  Validation Loss: {checkpoint['val_loss']:.4f}")

if 'history' in checkpoint:
    history = checkpoint['history']
    print(f"\nFinal Training Stats:")
    print(f"  Best Val Accuracy: {max(history['val_acc']):.2f}%")
    print(f"  Best Train Accuracy: {max(history['train_acc']):.2f}%")
    print(f"  Total Epochs Trained: {len(history['train_acc'])}")