import torch
import torch.nn as nn
import torch.optim as optim
from pathlib import Path
import sys

sys.path.append(str(Path(__file__).parent.parent))

from config import *
from models.resnet_model import create_model
from utils.data_loader import create_data_loaders
from tqdm import tqdm
import matplotlib.pyplot as plt
from sklearn.metrics import confusion_matrix, classification_report
import seaborn as sns
from collections import Counter


def train_epoch(model, dataloader, criterion, optimizer, device):
    """Train for one epoch"""
    model.train()
    running_loss = 0.0
    correct = 0
    total = 0

    pbar = tqdm(dataloader, desc="Training")
    for inputs, labels in pbar:
        inputs, labels = inputs.to(device), labels.to(device)

        optimizer.zero_grad()
        outputs = model(inputs)
        loss = criterion(outputs, labels)
        loss.backward()
        optimizer.step()

        running_loss += loss.item()
        _, predicted = torch.max(outputs.data, 1)
        total += labels.size(0)
        correct += (predicted == labels).sum().item()

        pbar.set_postfix({'loss': f'{loss.item():.4f}',
                          'acc': f'{100 * correct / total:.2f}%'})

    epoch_loss = running_loss / len(dataloader)
    epoch_acc = 100 * correct / total

    return epoch_loss, epoch_acc


def validate(model, dataloader, criterion, device):
    """Validate model"""
    model.eval()
    running_loss = 0.0
    correct = 0
    total = 0

    all_preds = []
    all_labels = []

    with torch.no_grad():
        pbar = tqdm(dataloader, desc="Validation")
        for inputs, labels in pbar:
            inputs, labels = inputs.to(device), labels.to(device)

            outputs = model(inputs)
            loss = criterion(outputs, labels)

            running_loss += loss.item()
            _, predicted = torch.max(outputs.data, 1)
            total += labels.size(0)
            correct += (predicted == labels).sum().item()

            all_preds.extend(predicted.cpu().numpy())
            all_labels.extend(labels.cpu().numpy())

            pbar.set_postfix({'loss': f'{loss.item():.4f}',
                              'acc': f'{100 * correct / total:.2f}%'})

    epoch_loss = running_loss / len(dataloader)
    epoch_acc = 100 * correct / total

    return epoch_loss, epoch_acc, all_preds, all_labels


def plot_confusion_matrix(y_true, y_pred, save_path):
    """Plot confusion matrix"""
    try:
        cm = confusion_matrix(y_true, y_pred)

        plt.figure(figsize=(8, 6))
        sns.heatmap(cm, annot=True, fmt='d', cmap='Blues',
                    xticklabels=['Benign', 'Malignant'],
                    yticklabels=['Benign', 'Malignant'])
        plt.ylabel('True Label')
        plt.xlabel('Predicted Label')
        plt.title('Confusion Matrix')
        plt.tight_layout()
        plt.savefig(save_path, dpi=300, bbox_inches='tight')
        print(f"📊 Confusion matrix saved to: {save_path}")
    except Exception as e:
        print(f"⚠️ Could not create confusion matrix: {e}")


def train_on_real_data():
    """Train on real CT scan data"""

    print("=" * 50)
    print("TRAINING ON REAL LUNG CANCER DATA")
    print("=" * 50)

    # Check for real data
    labels_csv = PROCESSED_DATA_DIR / "real_labels.csv"

    if not labels_csv.exists():
        print("\n❌ Real data not found!")
        print("\nPlease run these scripts first:")
        print("1. python scripts/download_real_data.py")
        print("2. python scripts/preprocess_real_data.py")
        return

    print("\n✅ Real data found!")

    # Load data
    print("\nLoading data...")
    train_loader, val_loader, test_loader = create_data_loaders(
        labels_csv,
        batch_size=BATCH_SIZE,
        num_workers=NUM_WORKERS
    )

    # Create model
    print("\nInitializing model...")
    model = create_model(
        num_classes=NUM_CLASSES,
        pretrained=PRETRAINED,
        device=DEVICE
    )

    # Use weighted loss for class imbalance
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(model.parameters(), lr=LEARNING_RATE)
    scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, mode='min', patience=5)

    history = {
        'train_loss': [],
        'train_acc': [],
        'val_loss': [],
        'val_acc': []
    }

    best_val_acc = 0.0
    patience_counter = 0
    early_stop_patience = 10

    print(f"\nStarting training for {NUM_EPOCHS} epochs...")
    print("=" * 50)

    for epoch in range(NUM_EPOCHS):
        print(f"\nEpoch {epoch + 1}/{NUM_EPOCHS}")
        print("-" * 50)

        train_loss, train_acc = train_epoch(model, train_loader, criterion, optimizer, DEVICE)
        val_loss, val_acc, val_preds, val_labels = validate(model, val_loader, criterion, DEVICE)

        scheduler.step(val_loss)
        current_lr = optimizer.param_groups[0]['lr']

        history['train_loss'].append(train_loss)
        history['train_acc'].append(train_acc)
        history['val_loss'].append(val_loss)
        history['val_acc'].append(val_acc)

        print(f"\nEpoch {epoch + 1} Results:")
        print(f"  Train Loss: {train_loss:.4f} | Train Acc: {train_acc:.2f}%")
        print(f"  Val Loss:   {val_loss:.4f} | Val Acc:   {val_acc:.2f}%")
        print(f"  Learning Rate: {current_lr:.6f}")

        # Save best model
        if val_acc > best_val_acc:
            best_val_acc = val_acc
            patience_counter = 0
            checkpoint_path = MODEL_DIR / "best_real_model.pth"
            torch.save({
                'epoch': epoch,
                'model_state_dict': model.state_dict(),
                'optimizer_state_dict': optimizer.state_dict(),
                'val_acc': val_acc,
                'val_loss': val_loss,
                'history': history
            }, checkpoint_path)
            print(f"  ✅ New best model saved! (Val Acc: {val_acc:.2f}%)")
        else:
            patience_counter += 1
            print(f"  ⏳ No improvement for {patience_counter} epoch(s)")
            if patience_counter >= early_stop_patience:
                print(f"\n⚠️  Early stopping triggered after {epoch + 1} epochs")
                break

    # Final evaluation
    print("\n" + "=" * 50)
    print("FINAL EVALUATION ON TEST SET")
    print("=" * 50)

    test_loss, test_acc, test_preds, test_labels = validate(model, test_loader, criterion, DEVICE)

    print(f"\nTest Loss: {test_loss:.4f}")
    print(f"Test Accuracy: {test_acc:.2f}%")

    # Classification report
    print("\n" + "=" * 50)
    print("DETAILED CLASSIFICATION REPORT")
    print("=" * 50)

    # Check unique classes in predictions
    unique_preds = set(test_preds)
    unique_labels = set(test_labels)

    print(f"\nUnique classes in test labels: {unique_labels}")
    print(f"Unique classes in predictions: {unique_preds}")

    pred_counts = Counter(test_preds)
    label_counts = Counter(test_labels)
    print(f"\nPrediction distribution: {dict(pred_counts)}")
    print(f"Actual label distribution: {dict(label_counts)}")

    # Only create report if we have both classes
    if len(unique_preds) > 1 and len(unique_labels) > 1:
        print("\n" + classification_report(test_labels, test_preds,
                                           target_names=['Benign', 'Malignant']))
    else:
        print("\n⚠️ WARNING: Model is predicting only one class!")
        print("This indicates the model hasn't learned to discriminate.")
        print("\nPossible causes:")
        print("  - Severe class imbalance in data")
        print("  - Insufficient training")
        print("  - Data quality issues")
        print("  - Model architecture not suitable")

    # Confusion matrix
    cm_path = MODEL_DIR / "confusion_matrix_real.png"
    plot_confusion_matrix(test_labels, test_preds, cm_path)

    # Plot training history
    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 4))

    ax1.plot(history['train_loss'], label='Train Loss')
    ax1.plot(history['val_loss'], label='Val Loss')
    ax1.set_xlabel('Epoch')
    ax1.set_ylabel('Loss')
    ax1.set_title('Training and Validation Loss')
    ax1.legend()
    ax1.grid(True)

    ax2.plot(history['train_acc'], label='Train Acc')
    ax2.plot(history['val_acc'], label='Val Acc')
    ax2.set_xlabel('Epoch')
    ax2.set_ylabel('Accuracy (%)')
    ax2.set_title('Training and Validation Accuracy')
    ax2.legend()
    ax2.grid(True)

    plt.tight_layout()
    plot_path = MODEL_DIR / "training_history_real.png"
    plt.savefig(plot_path, dpi=300, bbox_inches='tight')
    print(f"\n📊 Training plots saved to: {plot_path}")

    print("\n" + "=" * 50)
    print("✅ TRAINING COMPLETE!")
    print("=" * 50)
    print(f"Best validation accuracy: {best_val_acc:.2f}%")
    print(f"Final test accuracy: {test_acc:.2f}%")
    print(f"Model saved to: {MODEL_DIR}/best_real_model.pth")


if __name__ == "__main__":
    train_on_real_data()