import torch
import torch.nn as nn
# Define the CNN Model
class CNNModel(nn.Module):
    def __init__(self):
        super(CNNModel, self).__init__()
        # 1 input channel (grayscale), 32 output channels (filters), 3x3 kernel
        self.conv1 = nn.Conv2d(1, 32, kernel_size=3, stride=1, padding=1)
        # 32 input channels, 64 output channels (filters), 3x3 kernel
        self.conv2 = nn.Conv2d(32, 64, kernel_size=3, stride=1, padding=1)
        # Fully connected layer 1
        self.fc1 = nn.Linear(7 * 7 * 64, 128)  # After two 3x3 convolutions (28 -> 14 -> 7)
        # Fully connected layer 2 (output layer)
        self.fc2 = nn.Linear(128, 8)  # 8 classes

    def forward(self, x):
        x = torch.relu(self.conv1(x))  # Apply ReLU activation
        x = torch.max_pool2d(x, 2)  # Max pooling layer (2x2)
        x = torch.relu(self.conv2(x))
        x = torch.max_pool2d(x, 2)  # Max pooling layer (2x2)
        
        # Flatten the tensor for fully connected layers
        x = x.view(-1, 7 * 7 * 64)
        
        x = torch.relu(self.fc1(x))
        x = self.fc2(x)
        return x