# Contributing to Unity MCP Server

Thank you for your interest in contributing to Unity MCP Server! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Guidelines](#contributing-guidelines)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Reporting Issues](#reporting-issues)

## Code of Conduct

This project adheres to a code of conduct that we expect all contributors to follow. Please be respectful and constructive in all interactions.

## Getting Started

### Prerequisites

- Unity 2022.3 or higher
- [Claude Code CLI](https://docs.anthropic.com/en/docs/claude-code) for testing
- Git
- Basic knowledge of C# and Unity Editor scripting

### Development Setup

1. **Fork the repository**
   ```bash
   git clone https://github.com/your-username/unity-mcp.git
   cd unity-mcp
   ```

2. **Set up Unity project**
   - Open Unity Hub
   - Create a new Unity 2022.3+ project for testing
   - Copy the package to your test project:
     ```bash
     cp -r UnityPackage/com.mcp.unity /path/to/test/project/Packages/
     ```

3. **Configure Claude Code**
   ```bash
   claude mcp add --transport http unity-dev http://localhost:9876
   ```

4. **Run tests**
   ```bash
   ./diagnose.sh
   ./test_server.sh
   ./test_compilation.sh
   ```

## Contributing Guidelines

### Code Style

- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and single-purpose
- Use `var` for local variables when type is obvious

### Architecture Principles

- **Thread Safety**: All Unity API calls must happen on the main thread
- **Error Handling**: Graceful degradation, never crash Unity
- **Performance**: Minimize impact on Unity Editor performance
- **Security**: Validate all inputs, restrict dangerous operations

### File Organization

```
UnityPackage/com.mcp.unity/
├── Editor/
│   ├── MCPServer.cs              # Main HTTP server
│   ├── DynamicToolManager.cs     # Tool loading and execution
│   ├── UnityDynamicCompiler.cs   # C# code compilation
│   ├── MCPStatusWindow.cs        # GUI status window
│   ├── MCPPackageInstaller.cs    # First-run setup
│   ├── DynamicCompiler.cs        # Legacy compatibility
│   └── Tools/
│       ├── Default/              # Built-in tools
│       └── Custom/               # User-created tools
├── package.json                  # Unity package manifest
└── README.md                     # Package documentation
```

## Testing

### Manual Testing

1. **Server Connectivity**
   ```bash
   ./test_server.sh
   ```

2. **Code Compilation**
   ```bash
   ./test_compilation.sh
   ```

3. **Tool Management**
   ```bash
   ./test_dynamic_tools.sh
   ```

4. **Full Diagnostics**
   ```bash
   ./diagnose.sh
   ```

### Testing in Unity

1. Open the test Unity project
2. Check `Tools > Unity MCP > Server Status`
3. Verify all default tools load correctly
4. Test basic `execute_query` functionality
5. Test error handling (try invalid C# code)

### Testing with Claude Code

1. Verify Claude can connect to the server
2. Test basic tool execution
3. Test error scenarios
4. Test during Unity compilation

## Pull Request Process

### Before Submitting

1. **Test thoroughly**
   - Run all test scripts
   - Test with Claude Code
   - Test error scenarios
   - Verify Unity compatibility

2. **Code Quality**
   - Follow code style guidelines
   - Add appropriate comments
   - Update documentation if needed
   - No debug logs in production code

3. **Documentation**
   - Update API.md for new endpoints/tools
   - Update README.md for new features
   - Add examples if applicable

### Pull Request Guidelines

1. **Title**: Clear, descriptive title
2. **Description**: 
   - What changes were made
   - Why the changes were needed
   - How to test the changes
   - Any breaking changes

3. **Scope**: Keep PRs focused on a single feature/fix
4. **Tests**: Include test results or describe testing approach

### Review Process

- Code review by maintainers
- Testing in multiple Unity versions
- Documentation review
- Performance impact assessment

## Reporting Issues

### Bug Reports

Please include:
- Unity version
- MCP Server version
- Steps to reproduce
- Expected vs actual behavior
- Error messages/logs
- System information (OS, etc.)

### Feature Requests

Please include:
- Description of the feature
- Use case/motivation
- Proposed implementation approach
- Any alternatives considered

### Issue Templates

Use the provided issue templates when available. This helps us process issues more efficiently.

## Areas for Contribution

### High Priority

- **Unity Compatibility**: Testing with different Unity versions
- **Error Handling**: Improving error messages and recovery
- **Performance**: Optimizing server and compilation performance
- **Documentation**: Examples, tutorials, API documentation

### Medium Priority

- **Tool Development**: New built-in tools
- **Testing**: Automated tests, edge case testing
- **Platform Support**: Testing on different operating systems
- **IDE Integration**: Better development experience

### Low Priority

- **UI Improvements**: Better status window, configuration UI
- **Logging**: Better debug information and diagnostics
- **Code Quality**: Refactoring, cleanup, optimization

## Development Notes

### Key Components

1. **MCPServer.cs**: HTTP server handling MCP protocol
   - Runs on port 9876
   - Processes JSON-RPC requests
   - Manages tool execution

2. **DynamicToolManager.cs**: Tool management system
   - Loads tools from JSON files
   - Hot-reloading capability
   - Parameter substitution

3. **UnityDynamicCompiler.cs**: C# compilation engine
   - Compiles code snippets at runtime
   - Unity API access
   - Error handling

### Common Pitfalls

- **Threading**: Never call Unity APIs from background threads
- **Compilation**: Handle Unity domain reloads gracefully  
- **Error Handling**: Always provide meaningful error messages
- **Memory**: Clean up resources properly
- **Security**: Validate and sanitize all inputs

### Debugging Tips

- Use Unity Console for server logs
- Use `Tools > Unity MCP > Server Status` for monitoring
- Test with curl for HTTP debugging
- Use `./diagnose.sh` for comprehensive diagnostics

## Resources

- [Unity Editor Scripting](https://docs.unity3d.com/ScriptReference/Editor.html)
- [MCP Specification](https://modelcontextprotocol.io/)
- [Claude Code Documentation](https://docs.anthropic.com/en/docs/claude-code)
- [JSON-RPC 2.0](https://www.jsonrpc.org/specification)

## Questions?

- Open an issue for questions
- Check existing issues and documentation first
- Be specific about your use case

Thank you for contributing to Unity MCP Server!