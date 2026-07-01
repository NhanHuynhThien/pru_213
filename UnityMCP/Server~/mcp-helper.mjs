/**
 * MCP Unity Helper Script
 * Duy trì 1 session MCP duy nhất để thực thi nhiều lệnh
 * Usage: node scripts/mcp-helper.mjs <command> [args as JSON]
 * 
 * Commands:
 *   hierarchy                        - Đọc scene hierarchy
 *   assets [search]                  - Tìm assets
 *   add <assetPath> <x> <y> <z>     - Thêm asset vào scene
 *   save                             - Lưu scene
 *   logs                             - Đọc console logs
 *   tool <toolName> <argsJSON>       - Gọi bất kỳ tool nào
 */

import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Relative to UnityMCP/Server~/
const PROJECT_PATH = path.resolve(__dirname, '../..');
const SERVER_PATH = path.resolve(__dirname, 'build/index.js');

async function createClient() {
  const transport = new StdioClientTransport({
    command: 'C:/Users/ADMIN/AppData/Local/ms-playwright-go/1.57.0/node.exe',
    args: [SERVER_PATH],
    cwd: PROJECT_PATH
  });
  const client = new Client({ name: 'antigravity-helper', version: '1.0' });
  await client.connect(transport);
  return client;
}

async function main() {
  const [,, command, ...args] = process.argv;

  if (!command) {
    console.log('Usage: node mcp-helper.mjs <command> [args...]');
    console.log('Commands: hierarchy, assets, add, save, logs, tool');
    process.exit(0);
  }

  const client = await createClient();

  try {
    switch (command) {
      case 'hierarchy': {
        const res = await client.readResource({ uri: 'unity://scenes_hierarchy' });
        console.log(res.contents[0].text);
        break;
      }
      case 'assets': {
        const res = await client.readResource({ uri: 'unity://assets' });
        const data = JSON.parse(res.contents[0].text);
        const search = args[0]?.toLowerCase();
        const filtered = search
          ? (data.assets || []).filter(a => a.path?.toLowerCase().includes(search))
          : (data.assets || []).slice(0, 50);
        console.log(JSON.stringify(filtered, null, 2));
        break;
      }
      case 'add': {
        const [assetPath, x = '0', y = '0', z = '0'] = args;
        const result = await client.callTool({
          name: 'add_asset_to_scene',
          arguments: {
            assetPath,
            position: { x: parseFloat(x), y: parseFloat(y), z: parseFloat(z) }
          }
        });
        console.log(JSON.stringify(result.content[0]?.text));
        break;
      }
      case 'save': {
        const result = await client.callTool({ name: 'save_scene', arguments: {} });
        console.log(JSON.stringify(result.content[0]?.text));
        break;
      }
      case 'logs': {
        const res = await client.readResource({ uri: 'unity://logs/?offset=0&limit=20&includeStackTrace=false' });
        console.log(res.contents[0].text);
        break;
      }
      case 'tool': {
        const [toolName, argsJson = '{}'] = args;
        const result = await client.callTool({
          name: toolName,
          arguments: JSON.parse(argsJson)
        });
        console.log(JSON.stringify(result, null, 2));
        break;
      }
      case 'batch': {
        // Thực thi nhiều tool calls trong 1 session
        const batchJson = args[0];
        const batch = JSON.parse(batchJson);
        for (const item of batch) {
          console.log(`\n--- ${item.name} ---`);
          try {
            const result = await client.callTool({ name: item.name, arguments: item.arguments });
            console.log(result.content[0]?.text);
          } catch (e) {
            console.error(`Error: ${e.message}`);
          }
        }
        break;
      }
      default:
        console.error(`Unknown command: ${command}`);
    }
  } finally {
    await client.close();
  }
}

main().catch(e => {
  console.error('Fatal error:', e.message);
  process.exit(1);
});
