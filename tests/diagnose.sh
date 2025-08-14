#!/bin/bash

echo "Unity MCP Diagnostic"
echo "===================="
echo ""

# Check if Unity is running
echo "1. Checking if Unity is running..."
ps aux | grep Unity | grep -v grep > /dev/null
if [ $? -eq 0 ]; then
    echo "   ✓ Unity is running"
else
    echo "   ✗ Unity is NOT running - Please start Unity Editor"
fi
echo ""

# Check port 9876
echo "2. Checking port 9876..."
nc -z localhost 9876 2>/dev/null
if [ $? -eq 0 ]; then
    echo "   ✓ Port 9876 is open"
else
    echo "   ✗ Port 9876 is not listening"
    echo "   → In Unity: Tools > Unity MCP > Server Status"
    echo "   → Click 'Start Server' if it's not running"
fi
echo ""

# Test HTTP connection
echo "3. Testing HTTP connection..."
response=$(curl -s -X POST http://localhost:9876 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"initialize","params":{},"id":1}' 2>/dev/null)

if [ ! -z "$response" ]; then
    echo "   ✓ Server responded"
    echo "   Response: ${response:0:100}..."
else
    echo "   ✗ No response from server"
fi
echo ""

# Check Claude configuration
echo "4. Checking Claude configuration..."
if [ -f ~/.claude.json ]; then
    grep -q "unity" ~/.claude.json
    if [ $? -eq 0 ]; then
        echo "   ✓ Unity server found in Claude config"
        echo "   Configuration:"
        grep -A5 "unity" ~/.claude.json | head -6
    else
        echo "   ✗ Unity server not found in Claude config"
        echo "   → Run: claude mcp add --transport http unity http://localhost:9876"
    fi
else
    echo "   ✗ Claude config not found"
fi
echo ""

echo "5. Recommended actions:"
echo "   a) Make sure Unity Editor is running"
echo "   b) Open Tools > Unity MCP > Server Status"
echo "   c) Check that server shows as running on port 9876"
echo "   d) Click 'Restart Server' if needed"
echo "   e) Reconfigure Claude:"
echo "      claude mcp remove unity"
echo "      claude mcp add --transport http unity http://localhost:9876"