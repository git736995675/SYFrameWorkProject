# SYFrameWorkProject

ZSY Unity 游戏框架的管理工程（Unity 2022.3）。本仓库以 **UPM 包** 的形式发布框架，其他应用项目通过 **Git URL** 接入，无需拷贝代码。

## 仓库结构

```
Packages/
  com.zsy.framework/        # 框架本体（Runtime + Editor）
    Samples~/               # 隐形示例目录，面板中按需 Import
      Demo/                 # 示例场景、AB 资源、配置表、打包配置
      Spine/                # spine-unity 3.8 运行时
      SRDebugger/           # SRDebugger + SRF 调试面板
      SuperScrollView/      # 循环列表组件
      UGUIOutLine/          # UGUI 描边/渐变组件
      QHierarchy/           # Hierarchy 层级面板美化（编辑器工具）
      UniRx/                # UniRx 响应式扩展（框架代码不依赖，按需导入）
Assets/
  Editor/                   # 仅本管理工程使用（SpineSettings 等）
```

框架依赖：DOTween（已内置包内 ThirdParty/）、TextMeshPro（自动解析）。
框架代码不依赖 Samples 里的任何插件——它们是**可选导入**的应用层内容。

框架工程自身就是普通 Unity 工程：包位于 `Packages/` 下属于"嵌入包（embedded package）"，在本工程里可以直接编辑包内代码、调试 Samples 内容，改完 push 即发布。**注意：管理工程自己调试 Demo 时，需手动把 `Samples~/Demo` 拷贝到 `Assets/Samples/...` 位置**（`~` 后缀目录 Unity 不导入），或临时去掉 `~`。

## 应用项目接入

### 一步接入

应用项目 `Packages/manifest.json` 只加一行：

```json
{
  "dependencies": {
    "com.zsy.framework": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework"
  }
}
```

（或 Package Manager → `+` → Add package from git URL 粘贴同样内容）

### 按需导入 Demo / 第三方插件

接入后打开 **Window → Package Manager → 选中 ZSY Framework**，右侧面板 **Samples** 页会列出全部可选项，点 **Import** 即导入到 `Assets/Samples/ZSY Framework/<版本>/...`：

| Sample | 内容 | 什么时候导入 |
| --- | --- | --- |
| ZSY Demo | 示例场景、AB 资源目录、配置表、打包配置 | 学习框架用法 / 跑通打包流程 |
| Spine 动画运行时 | spine-unity 3.8 | 项目用 Spine 动画 |
| SRDebugger 调试面板 | SRDebugger + SRF | 需要真机调试面板 |
| SuperScrollView 循环列表 | 循环列表组件 | 长列表 UI |
| UGUI 描边/渐变组件 | UGUIOutLine | 需要描边/渐变效果 |
| QHierarchy 层级面板美化 | 编辑器 Hierarchy 增强 | 想要更强大的层级窗口 |
| UniRx 响应式扩展 | UniRx | 项目使用响应式编程 |

导入 Demo 后 AB 资源根路径已预置为导入位置（`Assets/Samples/ZSY Framework/0.1.0/ZSY Demo/AbRes`），直接打开 `SY-Tool/AssetBundle/AssetBundle资源编辑器` 即可使用。

### 锁定版本

URL 末尾追加 `#` 指定 tag 或 commit：

```
https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework#v0.1.0
```

- 最新版：不加（跟随 main）
- 锁定版本：`#v0.1.0`（推荐）
- 锁定提交：`#<commit-sha>`

### 升级 / 私有仓库

- 升级：改 manifest.json 里的 `#` 版本号，或 Package Manager 里 Remove 后重新 Add
- 私有仓库：配 SSH key（URL 换 `git@github.com:...`）或 PAT（`https://<TOKEN>@github.com/...`）

## 框架版本发布流程（管理工程侧）

1. 修改 `Packages/com.zsy.framework/package.json` 中的 `version`
2. 提交并 push
3. 打 tag：

```bash
git tag v0.2.0
git push origin v0.2.0
```

## 新插件引入规则

往框架里加第三方插件前先问：**框架代码用到它吗？**

- 用到 → 放框架包本体 ThirdParty/（目前：DOTween）
- 用不到 → 放 `Samples~/` 作为可选 Sample，应用按需 Import（目前：UniRx、Spine、SRDebugger、SuperScrollView、UGUIOutLine、QHierarchy）
- 插件必须自带或补上 asmdef（Runtime + Editor 成对），避免被罩进自动程序集

## 维护操作手册

### 新增一个第三方插件（如 SuperTween）

1. 把插件目录拷进 `Packages/com.zsy.framework/Samples~/SuperTween/`
2. 确认插件有 asmdef；没有就补一对（Runtime 放插件根，Editor 放插件 Editor 目录，Editor 引用 Runtime）
3. 在 `Packages/com.zsy.framework/package.json` 的 `samples` 数组加一项：

```json
{
  "displayName": "SuperTween 补间动画",
  "description": "轻量补间动画库。",
  "path": "Samples~/SuperTween"
}
```

> 注意：`displayName` 里不要有 `/`（Unity 拿它当导入目录名）；全局 gitignore 的 `*~` 规则会误伤 `Samples~`，本工程 `.gitignore` 已加例外，新文件正常 `git add` 即可。

4. `git add -A && git commit && git push`
5. 应用项目侧：删 `Library/PackageCache/com.zsy.framework@<hash>` 缓存重新拉取（或升级版本号），面板即出现新条目

### 修改 Demo / 已有插件

**在管理工程里改（用工具栏按钮一键切换）**：编辑器顶部工具栏（播放按钮右侧）有 **Samples** 按钮：

- 点击切换：`Samples`（橙色高亮 = 显示中，Unity 已导入 Demo/插件，可直接调试）⇄ `Samples`（普通态 = 隐藏，面板 Import 形态）
- 鼠标悬停有状态提示；**提交推送前必须切回隐藏态**（橙色警示就是为了提醒这个）
- 也可走菜单 `SY_Tools → Samples 显示与隐藏`，功能一致

> 显示态下 Unity 会把 Samples 内容当普通资产编译（生成 .meta 等），属于正常现象；切回隐藏态后 Unity 自动清理。应用项目里也有这个按钮，但点它无副作用（应用项目没有 `Packages/.../Samples~` 目录，只会日志提示）。

**在应用项目里改**（推荐日常用法）：Import 到应用项目后直接改 `Assets/Samples/...` 下的文件——但这是复制体，改动**不会回流**框架仓库。改完如果值得沉淀，把对应文件拷回管理工程的 `Samples~/` 提交推送。

### 发布新版本

改完代码后：

```bash
# 1. 提交
git add -A && git commit -m "更新 Demo"

# 2. 升版本号 (package.json 的 version, 如 0.1.0 → 0.2.0)
# 3. 打 tag 推送
git tag v0.2.0
git push origin main --tags
```

应用项目把 manifest URL 的后缀改成 `#v0.2.0` 即完成升级。不锁版本（跟随 main）的应用项目，删除 `Library/PackageCache` 缓存重新解析即可拿到最新。

> ⚠️ 陷阱提醒：`~/.gitignore_global` 里的 `*~` 规则会忽略 `Samples~/`，如果发现 Sample 内容没提交上去，用 `git ls-files | grep Samples~` 检查，必要时 `git add -f "Packages/com.zsy.framework/Samples~/"`。
