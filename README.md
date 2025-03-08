# SampleFind

## Overview
SampleFind is a Windows .NET application designed for efficient log file analysis. It allows users to load text or log files and create multiple filters to display only the lines that match specific conditions. With its powerful filtering and text processing capabilities, SampleFind helps users quickly identify patterns and extract relevant information from large log files.

## Key Features

### File Operations
- Load and analyze text files (.txt) and log files of any size
- Optimized handling of very large files with efficient memory management
- Save filtered or processed content to new files
- Automatic detection of file types

### Advanced Filtering
- Create and apply multiple custom filters simultaneously
- Enable/disable individual filters without deleting them
- Filter by text contains or exact match conditions
- Highlight matching lines with custom colors for each filter
- Save and load filter configurations for reuse
- Default filters can be loaded automatically on startup

### Text Processing Tools
- Clean Date Part: Remove timestamps or data before the first '|' character
- Combine Identical Lines: Consolidate consecutive identical lines with occurrence count
- Line numbers display option for easier reference
- Status bar showing total and filtered line counts with percentages

### User Interface
- Modern interface with rounded buttons and visual feedback
- Customizable filter panel that can be shown/hidden
- Responsive design that handles large files without freezing
- Real-time progress indicators for long operations

## Installation
```bash
# Download the latest release
# Run the installer and follow the on-screen instructions
```

## Usage

### Basic Operations
1. Launch SampleFind
2. Open a text or log file using File > Open
3. Use the View menu to show/hide line numbers or the filters panel
4. Create filters by clicking "Add Filter" in the filter panel
5. Apply filters to see matching lines
6. Save your results using File > Save Content

### Working with Filters
- Click "Add Filter" to create a new filter
- Select filter type (CONTAINS or EQUALS)
- Enter the text to filter by
- Choose a highlight color by clicking the color button
- Enable/disable filters using the checkbox
- Click "Apply Filters" to see the results
- Save your filter configuration for future use

### Using Text Processing Tools
- **Clean Date Part**: Removes everything before the first '|' character on each line
  - Useful for removing timestamps from log entries
  - Access via Tools > Clean Date Part

- **Combine Identical Lines**: Consolidates consecutive identical lines
  - Shows the count of occurrences (e.g., "Line content - 5")
  - Reduces visual clutter in logs with repetitive entries
  - Access via Tools > Combine Identical Lines

## Configuration
Filters can be configured with various matching conditions:
- Text contains/does not contain specific strings
- Exact text matching
- Each filter can be individually enabled or disabled
- Custom highlight colors for each filter

### Default Filters
Place a file named "default.json" in the application directory to automatically load a set of filters on startup.

## Requirements
- Windows operating system
- .NET Framework 4.7.2 or higher
- Minimum screen resolution: 1024x768
- 4GB RAM recommended for large files

## Development

### Development Environment
- Visual Studio 2019 or later (recommended)
- .NET Framework SDK 4.7.2 or higher
- Windows 10 or later for development

### Cursor Integration
When developing SampleFind with Cursor:
- Ensure proper Git configuration to avoid permission issues when pushing changes
- Use the solution file (WinFormsApp1.sln) to open the project in Cursor
- The main application code is in Form1.cs
- Custom controls are defined within the same file (RoundedButton, LineNumberRichTextBox)

### Project Structure
- **WinFormsApp1/**: Main project directory
  - **Form1.cs**: Contains the main application logic and UI components
  - **Program.cs**: Application entry point
  - **FilterCondition.cs**: Data model for filter conditions
  - **FilterData.cs**: Data model for serializable filter data

### Code Conventions
- Use consistent naming conventions (PascalCase for public members, camelCase for private)
- Add XML documentation comments for public methods and classes
- Follow the existing pattern for event handlers and UI updates
- Maintain separation between UI logic and data processing

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License
[License information]