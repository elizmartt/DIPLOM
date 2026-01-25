"""
Quick utility to check the saved model structure
"""
import torch

MODEL_PATH = 'neurological_resnet18_best.pth'

# Load the state dict
state_dict = torch.load(MODEL_PATH, map_location='cpu', weights_only=False)

# Check the final layer to determine number of classes
if 'fc.weight' in state_dict:
    num_classes = state_dict['fc.weight'].shape[0]
    print(f"✓ Model has {num_classes} output classes")
    print(f"  Final layer shape: {state_dict['fc.weight'].shape}")
elif isinstance(state_dict, dict) and 'model_state_dict' in state_dict:
    num_classes = state_dict['model_state_dict']['fc.weight'].shape[0]
    print(f"✓ Model has {num_classes} output classes")
else:
    print("Model structure:")
    print(type(state_dict))
    if isinstance(state_dict, dict):
        print("Keys:", list(state_dict.keys())[:10])