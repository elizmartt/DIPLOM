import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
from torchvision import transforms, models
from PIL import Image
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, confusion_matrix, accuracy_score
import os
from pathlib import Path
import joblib


class NeurologicalImageDataset(Dataset):
    def __init__(self, image_paths, labels, transform=None):
        self.image_paths = image_paths
        self.labels = labels
        self.transform = transform

    def __len__(self):
        return len(self.image_paths)

    def __getitem__(self, idx):
        img_path = self.image_paths[idx]
        image = Image.open(img_path).convert('RGB')
        label = self.labels[idx]

        if self.transform:
            image = self.transform(image)

        return image, label


class NeurologicalResNet18:
    def __init__(self, num_classes=8, device=None):
        self.device = device if device else torch.device('cuda' if torch.cuda.is_available() else 'cpu')
        self.num_classes = num_classes

        self.model = models.resnet18(pretrained=True)
        num_features = self.model.fc.in_features
        self.model.fc = nn.Linear(num_features, num_classes)
        self.model = self.model.to(self.device)

        self.train_transform = transforms.Compose([
            transforms.Resize((224, 224)),
            transforms.RandomHorizontalFlip(),
            transforms.RandomRotation(10),
            transforms.ColorJitter(brightness=0.2, contrast=0.2),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406],
                                 std=[0.229, 0.224, 0.225])
        ])

        self.val_transform = transforms.Compose([
            transforms.Resize((224, 224)),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406],
                                 std=[0.229, 0.224, 0.225])
        ])

    def prepare_data_from_folders(self, data_dir, test_size=0.2):
        image_paths = []
        labels = []
        label_map = {}

        data_path = Path(data_dir)

        if (data_path / 'training').exists() and (data_path / 'testing').exists():
            train_path = data_path / 'training'
            test_path = data_path / 'testing'

            class_dirs = sorted([d for d in train_path.iterdir() if d.is_dir()])
            for idx, class_dir in enumerate(class_dirs):
                label_map[idx] = class_dir.name

                for img_file in class_dir.glob('*.[jp][pn][g]'):
                    image_paths.append(str(img_file))
                    labels.append(idx)

                test_class_dir = test_path / class_dir.name
                if test_class_dir.exists():
                    for img_file in test_class_dir.glob('*.[jp][pn][g]'):
                        image_paths.append(str(img_file))
                        labels.append(idx)
        else:
            class_dirs = sorted([d for d in data_path.iterdir() if d.is_dir()])
            for idx, class_dir in enumerate(class_dirs):
                label_map[idx] = class_dir.name
                for img_file in class_dir.glob('*.[jp][pn][g]'):
                    image_paths.append(str(img_file))
                    labels.append(idx)

        print(f"Found {len(image_paths)} images across {len(label_map)} classes")
        print(f"Label mapping: {label_map}")

        X_train, X_val, y_train, y_val = train_test_split(
            image_paths, labels, test_size=test_size, stratify=labels, random_state=42
        )

        return X_train, X_val, y_train, y_val, label_map

    def create_dataloaders(self, X_train, X_val, y_train, y_val, batch_size=32):
        train_dataset = NeurologicalImageDataset(X_train, y_train, self.train_transform)
        val_dataset = NeurologicalImageDataset(X_val, y_val, self.val_transform)

        train_loader = DataLoader(train_dataset, batch_size=batch_size,
                                  shuffle=True, num_workers=2)
        val_loader = DataLoader(val_dataset, batch_size=batch_size,
                                shuffle=False, num_workers=2)

        return train_loader, val_loader

    def train(self, train_loader, val_loader, epochs=25, learning_rate=0.001):
        criterion = nn.CrossEntropyLoss()
        optimizer = optim.Adam(self.model.parameters(), lr=learning_rate)
        scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, 'min', patience=3)

        best_val_acc = 0.0
        train_losses, val_losses = [], []
        train_accs, val_accs = [], []

        for epoch in range(epochs):
            self.model.train()
            train_loss = 0.0
            train_correct = 0
            train_total = 0

            for images, labels in train_loader:
                images, labels = images.to(self.device), labels.to(self.device)

                optimizer.zero_grad()
                outputs = self.model(images)
                loss = criterion(outputs, labels)
                loss.backward()
                optimizer.step()

                train_loss += loss.item()
                _, predicted = torch.max(outputs.data, 1)
                train_total += labels.size(0)
                train_correct += (predicted == labels).sum().item()

            train_loss = train_loss / len(train_loader)
            train_acc = 100 * train_correct / train_total

            self.model.eval()
            val_loss = 0.0
            val_correct = 0
            val_total = 0

            with torch.no_grad():
                for images, labels in val_loader:
                    images, labels = images.to(self.device), labels.to(self.device)
                    outputs = self.model(images)
                    loss = criterion(outputs, labels)

                    val_loss += loss.item()
                    _, predicted = torch.max(outputs.data, 1)
                    val_total += labels.size(0)
                    val_correct += (predicted == labels).sum().item()

            val_loss = val_loss / len(val_loader)
            val_acc = 100 * val_correct / val_total

            scheduler.step(val_loss)

            train_losses.append(train_loss)
            val_losses.append(val_loss)
            train_accs.append(train_acc)
            val_accs.append(val_acc)

            print(f'Epoch [{epoch + 1}/{epochs}]')
            print(f'Train Loss: {train_loss:.4f}, Train Acc: {train_acc:.2f}%')
            print(f'Val Loss: {val_loss:.4f}, Val Acc: {val_acc:.2f}%')
            print('-' * 60)

            if val_acc > best_val_acc:
                best_val_acc = val_acc
                torch.save(self.model.state_dict(), 'neurological_resnet18_best.pth')
                print(f'Saved best model with validation accuracy: {val_acc:.2f}%')

        return {
            'train_losses': train_losses,
            'val_losses': val_losses,
            'train_accs': train_accs,
            'val_accs': val_accs,
            'best_val_acc': best_val_acc
        }

    def evaluate(self, val_loader, label_map):
        self.model.eval()
        all_preds = []
        all_labels = []

        with torch.no_grad():
            for images, labels in val_loader:
                images = images.to(self.device)
                outputs = self.model(images)
                _, predicted = torch.max(outputs.data, 1)

                all_preds.extend(predicted.cpu().numpy())
                all_labels.extend(labels.numpy())

        accuracy = accuracy_score(all_labels, all_preds)
        conf_matrix = confusion_matrix(all_labels, all_preds)

        target_names = [label_map[i] for i in sorted(label_map.keys())]
        class_report = classification_report(all_labels, all_preds,
                                             target_names=target_names)

        print("\n" + "=" * 60)
        print("MODULE 1 - IMAGING ANALYSIS RESULTS")
        print("=" * 60)
        print(f"\nOverall Accuracy: {accuracy * 100:.2f}%")
        print(f"\nConfusion Matrix:\n{conf_matrix}")
        print(f"\nClassification Report:\n{class_report}")

        return accuracy, conf_matrix, class_report

    def save_model(self, path='neurological_resnet18.pth', label_map_path='neurological_label_map.pkl'):
        torch.save(self.model.state_dict(), path)
        joblib.dump(label_map, label_map_path)
        print(f"Model saved to {path}")

    def load_model(self, path='neurological_resnet18.pth'):
        self.model.load_state_dict(torch.load(path, map_location=self.device))
        self.model.eval()
        print(f"Model loaded from {path}")


if __name__ == "__main__":
    model = NeurologicalResNet18(num_classes=8)

    data_dir = r"C:\Users\Eliza\PycharmProjects\DIPLOM\ml\Brain_tumor\Data\dataset"
    X_train, X_val, y_train, y_val, label_map = model.prepare_data_from_folders(data_dir)

    train_loader, val_loader = model.create_dataloaders(
        X_train, X_val, y_train, y_val, batch_size=32
    )

    print("Starting training...")
    history = model.train(train_loader, val_loader, epochs=25, learning_rate=0.001)

    print("\nEvaluating model...")
    model.load_model('neurological_resnet18_best.pth')
    accuracy, conf_matrix, class_report = model.evaluate(val_loader, label_map)

    model.save_model('neurological_resnet18_final.pth')