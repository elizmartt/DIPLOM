import torch
import torch.nn as nn
from torchvision.models import resnet18, ResNet18_Weights

class LungCancerResNet(nn.Module):
    """ResNet18 for Lung Cancer Classification"""
    
    def __init__(self, num_classes=2, pretrained=True):
        super(LungCancerResNet, self).__init__()
        
        if pretrained:
            self.resnet = resnet18(weights=ResNet18_Weights.IMAGENET1K_V1)
        else:
            self.resnet = resnet18(weights=None)
        
        num_features = self.resnet.fc.in_features
        self.resnet.fc = nn.Linear(num_features, num_classes)
    
    def forward(self, x):
        return self.resnet(x)

def create_model(num_classes=2, pretrained=True, device='cuda'):
    """Create and initialize model"""
    model = LungCancerResNet(num_classes=num_classes, pretrained=pretrained)
    model = model.to(device)
    
    print(f" Model created: ResNet18")
    print(f"   - Pretrained: {pretrained}")
    print(f"   - Num classes: {num_classes}")
    print(f"   - Device: {device}")
    
    total_params = sum(p.numel() for p in model.parameters())
    trainable_params = sum(p.numel() for p in model.parameters() if p.requires_grad)
    
    print(f"   - Total parameters: {total_params:,}")
    print(f"   - Trainable parameters: {trainable_params:,}")
    
    return model