# RetroPalette

RetroPalette is a modern pixel art editor designed for creating and editing retro-style graphics. It provides an intuitive interface with essential tools for pixel art creation, including a color picker, various drawing tools, and support for sprite manipulation.

## Features

- Pixel-perfect drawing tools (Pen, Eraser, Bucket Fill)
- Color picker with HSV color selection
- Marquee selection tool for moving and copying pixel regions
- Undo/Redo functionality
- Zoom and pan controls
- Sprite resizing and canvas management
- Export to various image formats
- Support for loading and saving sprites
- Customizable background patterns

## Requirements

- Windows 10 or later
- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later (for development)

## Download and Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/RetroPalette.git
cd RetroPalette
```

2. Open the solution in Visual Studio:
   - Open `RetroPalette.sln` in Visual Studio
   - Build the solution (F6 or Build > Build Solution)
   - Run the application (F5 or Debug > Start Debugging)

## Building from Source

1. Ensure you have Visual Studio 2019 or later installed with .NET desktop development workload.

2. Clone the repository and open the solution:
```bash
git clone https://github.com/yourusername/RetroPalette.git
cd RetroPalette
```

3. Build the solution:
   - Open `RetroPalette.sln` in Visual Studio
   - Select the desired build configuration (Debug/Release)
   - Build the solution (F6 or Build > Build Solution)

4. The compiled executable will be in the `bin/Debug` or `bin/Release` directory.

## Contributing

We welcome contributions to RetroPalette! Here's how you can help:

1. Fork the repository
2. Create a new branch for your feature or bugfix:
```bash
git checkout -b feature/your-feature-name
```

3. Make your changes and commit them:
```bash
git commit -m "Description of your changes"
```

4. Push your branch to your fork:
```bash
git push origin feature/your-feature-name
```

5. Create a Pull Request from your fork to the main repository

### Development Guidelines

- Follow the existing code style and naming conventions
- Add XML documentation comments for new public methods and classes
- Write clear commit messages
- Test your changes thoroughly
- Update documentation as needed

### Project Structure

- `RetroPalette/` - Main project directory
  - `Form1.cs` - Main application form and UI logic
  - `PixelCanvas.cs` - Custom control for pixel art editing
  - `ColorSelector.cs` - Custom color picker control
  - `BackgroundSettings.cs` - Background pattern configuration

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Thanks to all contributors who have helped improve RetroPalette
- Inspired by classic pixel art tools and modern design principles

## Support

If you encounter any issues or have questions:
1. Check the [Issues](https://github.com/yourusername/RetroPalette/issues) page
2. Create a new issue if your problem hasn't been reported
3. Join our community discussions

## Roadmap

- [ ] Layer support
- [ ] Animation timeline
- [ ] Custom brush patterns
- [ ] Export to sprite sheet
- [ ] More file format support
- [ ] Plugin system

## Contact

For questions or suggestions, please open an issue or contact the maintainers.

