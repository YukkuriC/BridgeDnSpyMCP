# 编译规范

## 编译工具

统一使用 `dotnet` CLI，禁止使用 IDE 内置编译或直接调用 MSBuild。

编译前若 dnSpy MCP 处于在线状态，须先通过 `close_self` 指令终止其运行。

### 常用命令

```powershell
# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 编译指定配置
dotnet build -c Release

# 运行项目
dotnet run
```
