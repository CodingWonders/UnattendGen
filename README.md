![Banner](./res/banner.png)

**UnattendGen** is a console application that generates unattended answer files given certain parameters.

## Licenses

- **Program:** GNU GPLv3
- **Library:** MIT

This project uses Christoph Schneegans' unattended answer file generation library for core functionality. Its license file, including its source files, can be found in the `Library` folder.

## Building

**Requirements:** [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.303-windows-x64-installer), Visual Studio 2022

### Initializing submodules

This repository uses submodules, which you can initialize by running the following commands:

- `git submodule init` (clones all submodules)
- `git submodule update` (updates the submodules to their latest commits)

- You can also clone the repository while passing the `--recurse-submodules` flag for a simple one-liner!

Learn more [here](https://git-scm.com/book/en/v2/Git-Tools-Submodules)

### Building the project

1. Open the project
2. Click Build -> Build solution, or press CTRL+SHIFT+B

## Contributing

1. Fork this repository
2. Work on your changes **AND TEST THEM**
3. Commit the changes and push them
4. Make a pull request
