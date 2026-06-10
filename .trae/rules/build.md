# 编译规范

## 编译工具

编译时使用 `dotnet` 命令行工具，不使用 IDE 内置编译或 MSBuild 直接调用。

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
