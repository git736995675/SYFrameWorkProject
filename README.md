# SYFrameWorkProject

ZSY Unity 游戏框架的管理工程（Unity 2022.3）。本仓库以 **UPM 包** 的形式发布框架，其他应用项目通过 **Git URL** 接入，无需拷贝代码。

## 仓库结构

```
Packages/
  com.zsy.framework/                    # 框架本体（Runtime + Editor，内置依赖 UniRx / DOTween）
  com.zsy.framework.demo/               # Demo：示例场景、AB 资源、打包配置
  com.zsy.thirdparty.spine/             # 可选：Spine 动画（官方包结构）
  com.zsy.thirdparty.srdebugger/        # 可选：SRDebugger + SRF 调试面板
  com.zsy.thirdparty.superscrollview/   # 可选：SuperScrollView 循环列表
  com.zsy.thirdparty.uguioutline/       # 可选：UGUI 描边/渐变组件
Assets/
  Editor/                               # 仅本管理工程使用（SpineSettings 等）
  Editor Plugins/QHierarchy/            # 仅本管理工程使用（Hierarchy 编辑器美化）
```

拆分原则：**框架代码依赖的插件（UniRx、DOTween）留在框架包内；框架不依赖的插件拆为可选包**，应用项目按需引入。

框架工程自身就是普通 Unity 工程：包位于 `Packages/` 下属于"嵌入包（embedded package）"，在本工程里可以直接编辑包内代码、打开 Demo 场景调试，改完 push 即发布。

## 应用项目接入

编辑应用项目的 `Packages/manifest.json`。**每个包一行，按需增减**（包之间的 `dependencies` 不支持 git URL，无法自动带出，需显式声明）：

```json
{
  "dependencies": {
    "com.zsy.framework": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework",
    "com.zsy.framework.demo": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework.demo",
    "com.zsy.thirdparty.spine": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.thirdparty.spine",
    "com.zsy.thirdparty.srdebugger": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.thirdparty.srdebugger",
    "com.zsy.thirdparty.superscrollview": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.thirdparty.superscrollview",
    "com.zsy.thirdparty.uguioutline": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.thirdparty.uguioutline"
  }
}
```

常用组合：

| 需求 | 引入 |
| --- | --- |
| 最小接入 | `framework`（含 UniRx/DOTween） |
| 需要看示例 | + `framework.demo` |
| 用 Spine 动画 | + `spine` |
| 要调试面板 | + `srdebugger` |
| 长列表 UI | + `superscrollview`、`uguioutline` |

> 锁版本：任意一行的 URL 末尾追加 `#v1.0.0`（tag）或 `#<commit>`。所有包共用同一个仓库，锁的是同一份代码快照。

### 升级

- **Package Manager 窗口**：选中包 → Remove 后重新 Add by Git URL（或直接改 manifest.json 里的 `#` 版本号）
- Unity 2022.3 会把 Git 包缓存在 `Library/PackageCache`，切换版本号后自动重新拉取

### 私有仓库

如果仓库设为 private，需配置 SSH key 或使用 GitHub PAT：

```
https://<TOKEN>@github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework
```

## 框架版本发布流程（管理工程侧）

1. 修改对应包的 `package.json` 中的 `version`
2. 提交并 push
3. 打 tag 并推送：

```bash
git tag v0.2.0
git push origin main --tags
```

应用项目即可通过 `#v0.2.0` 使用新版本。

## 新插件引入规则

往框架里加第三方插件前先回答一个问题：**框架代码用到它吗？**

- 用到 → 放 `com.zsy.framework/ThirdParty/`（目前：UniRx、DOTween）
- 用不到 → 建独立 `com.zsy.thirdparty.<name>/` 包，或放本工程 `Assets/Editor Plugins/`（纯编辑器工具）
- 插件必须自带或补上 asmdef（Runtime + Editor 成对），避免被罩进自动程序集
