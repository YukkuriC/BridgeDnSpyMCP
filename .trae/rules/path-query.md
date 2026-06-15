---
alwaysApply: false
description: 需要查询依赖文件时
---
# 路径查询规则

## 数据源

项目根目录 `path.props`，定义了 `DnSpyPath` 属性（dnSpy 安装根目录）。

查询 dnSpy 相关路径时，须通过 `$(DnSpyPath)` 拼接获取，禁止从源码中搜索或硬编码。

## 常用路径

| 用途 | 拼接规则 |
|------|----------|
| 主程序（exe） | `$(DnSpyPath)\dnSpy.exe` |
| 内部 DLL 目录 | `$(DnSpyPath)\` |
