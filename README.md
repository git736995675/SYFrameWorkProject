# SYFrameWorkProject

ZSY Unity 游戏框架的管理工程（Unity 2022.3）。本仓库以 **UPM 包** 的形式发布框架，其他应用项目通过 **Git URL** 接入，无需拷贝代码。

## 仓库结构

```
Packages/
  com.zsy.framework/        # 框架本体（Runtime + Editor + ThirdParty）
  com.zsy.framework.demo/   # Demo：示例场景、AB 资源、配置表、打包配置
Assets/
  Editor/                   # 仅本管理工程使用（SpineSettings 等）
```

框架工程自身就是普通 Unity 工程：包位于 `Packages/` 下属于"嵌入包（embedded package）"，在本工程里可以直接编辑包内代码、打开 Demo 场景调试，改完 push 即发布。

## 应用项目接入

### 方式一：只接入框架

编辑应用项目的 `Packages/manifest.json`，加入：

```json
{
  "dependencies": {
    "com.zsy.framework": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework"
  }
}
```

### 方式二：框架 + Demo 一起接入

```json
{
  "dependencies": {
    "com.zsy.framework": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework",
    "com.zsy.framework.demo": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework.demo"
  }
}
```

> 注意：demo 包声明了对框架包的依赖，单独引用 demo 时 Unity 会要求先解析框架包，建议两个都显式写上。
> ThirdParty（Spine / UniRx / QHierarchy）位于框架包内部，随框架一起导入，无需单独引用。

### 锁定版本（推荐）

在 URL 末尾追加 `#` 指定 tag、分支或 commit：

```
https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework#v1.0.0
```

| 目标     | URL 后缀            | 说明                         |
| -------- | ------------------- | ---------------------------- |
| 最新版   | （不加）            | 跟随 main，`#main` 等价      |
| 锁定版本 | `#v1.0.0`           | 框架发布 tag 后应用自主升级  |
| 锁定提交 | `#9a0d258`          | 精确固定到某次提交           |

### 升级

- **Package Manager 窗口**：选中包 → Remove 后重新 Add by Git URL（或直接改 manifest.json 里的 `#` 版本号）
- Unity 2022.3 会把 Git 包缓存在 `Library/PackageCache`，切换版本号后自动重新拉取

### 私有仓库

如果仓库设为 private，需配置 SSH key 或使用 GitHub PAT：

```
https://<TOKEN>@github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework
```

## 框架版本发布流程（管理工程侧）

1. 修改 `Packages/com.zsy.framework/package.json` 中的 `version`（如 `0.1.0` → `0.2.0`）
2. 提交并 push
3. 打 tag 并推送：

```bash
git tag v0.2.0
git push origin main --tags
```

应用项目即可通过 `#v0.2.0` 使用新版本。
