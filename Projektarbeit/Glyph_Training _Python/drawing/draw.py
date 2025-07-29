import tkinter as tk
from PIL import Image, ImageDraw, ImageTk
from pathlib import Path
import yaml

# Parameters
IMAGE_SIZE = 64  # Final image size to save
CANVAS_SIZE = 600  # Size of the drawing canvas
ELEMENTS = ["air", "earth", "fire", "water"]  # Categories to draw
DATASET_DIR = "dataset/own_dataset"  # Path to save drawn images
ELEMENT_IMAGES = {  # Reference images for each element
    "air": f"{DATASET_DIR}/Ground_Truth/AIR.png",
    "earth": f"{DATASET_DIR}/Ground_Truth/EARTH.png",
    "fire": f"{DATASET_DIR}/Ground_Truth/FIRE.png",
    "water": f"{DATASET_DIR}/Ground_Truth/WATER.png",
}
YAML_FILE = (
    "element_counts.yml"  # File to store how many images have been drawn
)


# noinspection PyTypeChecker
class ElementDrawerApp:
    def __init__(self, root):
        self.reference_frame = None
        self.root = root
        self.root.geometry("600x1000")
        self.root.title("Element Drawer")

        # Track drawings per element in current cycle
        self.current_element = None
        self.saved_counts = None
        self.total_counts = None
        self.load_counts()  # Load previously saved counts from YAML

        # Initialize UI
        self.setup_ui()

    # New YAML methods (minimal addition)
    def load_counts(self):
        # Load saved drawing counts for each element from YAML file
        with open(YAML_FILE, "r") as f:
            self.saved_counts = yaml.safe_load(f)
            self.total_counts = sum(
                list(self.saved_counts.values())
            )  # Total number of all drawings

    def save_counts(self):
        # Save updated counts back to YAML file
        with open(YAML_FILE, "w") as f:
            yaml.dump(self.saved_counts, f)

    def on_close(self):
        # Save counts and close the application window
        self.save_counts()
        self.root.destroy()

    def save(self):
        # Save the current drawing to the correct element folder
        if not self.current_element:
            print("No element selected.")
            return
        # Only change: Use saved_counts for file naming
        category_folder = Path(DATASET_DIR) / self.current_element
        category_folder.mkdir(parents=True, exist_ok=True)
        next_index = self.saved_counts.get(self.current_element, 0)
        img_path = category_folder / f"img_{next_index:04d}.png"

        # Resize and save the image
        img_resized = self.img.copy().resize((IMAGE_SIZE, IMAGE_SIZE))
        img_resized.save(img_path)
        print(f"Saved to {self.current_element} folder: {img_path}")

        # Update both counts
        self.saved_counts[self.current_element] += 1
        self.total_counts = sum(list(self.saved_counts.values()))
        self.count_label.config(text=f"count: {self.total_counts}")
        self.save_counts()
        self.clear()

    def show_next_element(self):
        # Select random element from those that need more drawings
        self.current_element = ELEMENTS[
            self.total_counts % 4
        ]  # Simple round-robin selection
        image_path = ELEMENT_IMAGES[self.current_element]

        try:
            # Load and resize the element image
            element_img = Image.open(image_path)
            element_img = element_img.resize((200, 200))
            element_photo = ImageTk.PhotoImage(element_img)

            # Update the reference image label
            self.reference_label.config(image=element_photo)
            self.reference_label.image = (
                element_photo  # Store a reference to avoid garbage collection
            )
        except FileNotFoundError:
            print("Error: Could not find {image_path}")
            self.current_element = None

    def setup_ui(self):
        # Reference image frame
        self.reference_frame = tk.Frame(self.root)
        self.count_label = tk.Label(
            self.reference_frame, text=f"count: {self.total_counts}", font=("Arial", 12)
        )
        self.reference_text = tk.Label(
            self.reference_frame, text="Draw: ", font=("Arial", 14)
        )
        self.reference_label = tk.Label(self.reference_frame)
        self.show_next_element()  # Display first element to draw
        self.count_label.pack()
        self.reference_text.pack()
        self.reference_label.pack(pady=10)

        # Progress display
        self.progress_frame = tk.Frame(self.root)
        self.progress_label = tk.Label(self.progress_frame, text="", justify=tk.LEFT)
        self.progress_label.pack()

        # Canvas
        self.canvas = tk.Canvas(
            self.root, bg="white", height=CANVAS_SIZE, width=CANVAS_SIZE
        )
        self.canvas.bind("<B1-Motion>", self.mouse_event)  # Bind drawing to mouse drag
        self.img = Image.new(
            "L", (CANVAS_SIZE, CANVAS_SIZE), "white"
        )  # Create blank image
        self.draw_img = ImageDraw.Draw(self.img)  # ImageDraw for drawing on the image

        # Buttons
        self.button_frame = tk.Frame(self.root)
        self.clear_button = tk.Button(
            self.button_frame, text="Clear", command=self.clear
        )
        self.save_button = tk.Button(self.button_frame, text="Save", command=self.save)

        # Layout
        self.reference_frame.pack(pady=5)
        self.progress_frame.pack(pady=5)
        self.canvas.pack(pady=10)
        self.clear_button.pack(side="left", padx=5)
        self.save_button.pack(side="left", padx=5)
        self.button_frame.pack(pady=5)

    def clear(self):
        # Clear the canvas and image, and load the next element
        self.canvas.delete("all")
        self.draw_img.rectangle([0, 0, CANVAS_SIZE, CANVAS_SIZE], fill="white")
        self.show_next_element()

    def mouse_event(self, event):
        # Draw on both canvas and PIL image using the mouse position
        x, y = event.x, event.y
        brushsize = 30
        self.canvas.create_oval(
            x, y, x + 1, y + 1, fill="black", outline="black", width=brushsize
        )
        self.draw_img.ellipse(
            [x - brushsize, y - brushsize, x + brushsize, y + brushsize], fill="black", outline="black"
        )

# Initialize and run application
root = tk.Tk()
app = ElementDrawerApp(root)
root.mainloop()
