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
Assets/
  Editor/                   # 仅本管理工程使用（SpineSettings 等）
  Editor Plugins/QHierarchy/  # 仅本管理工程使用（Hierarchy 美化）
```

框架依赖：UniRx、DOTween（已内置包内）、TextMeshPro（自动解析）。
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

- 用到 → 放框架包本体（目前：UniRx、DOTween）
- 用不到 → 放 `Samples~/` 作为可选 Sample，应用按需 Import
- 插件必须自带或补上 asmdef（Runtime + Editor 成对），避免被罩进自动程序集
