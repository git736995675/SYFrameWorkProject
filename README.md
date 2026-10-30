# SYFrameWorkProject

ZSY Unity 游戏框架的管理工程（Unity 2022.3）。本仓库以 **UPM 包** 的形式发布框架，其他应用项目通过 **Git URL** 接入，无需拷贝代码。

## 仓库结构

```
Packages/
  com.zsy.framework/                    # 框架本体（Runtime + Editor + 内置依赖）
    Samples~/Demo/                      # Demo（面板 Import 导入）
  com.zsy.framework.demo/               # ← 同一份 Demo，也提供独立包形式
  com.zsy.thirdparty.spine/             # 可选：Spine 动画（spine-unity 3.8）
  com.zsy.thirdparty.srdebugger/        # 可选：SRDebugger + SRF 调试面板
  com.zsy.thirdparty.superscrollview/   # 可选：SuperScrollView 循环列表
  com.zsy.thirdparty.uguioutline/       # 可选：UGUI 描边/渐变组件
Assets/
  Editor/                               # 仅本管理工程使用（SpineSettings 等）
  Editor Plugins/QHierarchy/            # 仅本管理工程使用（Hierarchy 美化）
```

- **Demo 双形态**：既在框架包 `Samples~/Demo`（面板 Import 一键导入），也是独立包 `com.zsy.framework.demo`（manifest 引入）——内容一致，选择顺手的即可
- **第三方插件为独立可选包**：框架代码不依赖它们；Spine/SRDebugger/SuperScrollView/UGUIOutLine 都是"冻结的第三方代码"，独立成包避免 `Samples~` 的 gitignore 陷阱，也不与框架版本耦合
- **框架内置依赖**：UniRx、DOTween（框架代码真实使用）；TextMeshPro（自动解析）

## 应用项目接入

### 第一步：接入框架（必选）

`Packages/manifest.json` 加入：

```json
{
  "dependencies": {
    "com.zsy.framework": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework"
  }
}
```

接入后 Package Manager 面板中 ZSY Framework 的**描述里列出了全部配套包**，也有 View documentation 按钮直达本 README。

### 第二步：按需添加配套包（都是可选）

| 包 | manifest 键 | 用途 |
| --- | --- | --- |
| Demo（独立包形式） | `com.zsy.framework.demo` | 示例场景、AB 资源、配置表、打包配置 |
| Spine 动画 | `com.zsy.thirdparty.spine` | spine-unity 3.8 运行时 |
| SRDebugger 调试面板 | `com.zsy.thirdparty.srdebugger` | SRDebugger + SRF 真机调试 |
| SuperScrollView 循环列表 | `com.zsy.thirdparty.superscrollview` | 长列表 UI |
| UGUI 描边渐变 | `com.zsy.thirdparty.uguioutline` | UGUI 描边/渐变效果 |

URL 规则统一为 `https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/<目录名>`，示例（加 Demo + SRDebugger）：

```json
{
  "dependencies": {
    "com.zsy.framework": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework",
    "com.zsy.framework.demo": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework.demo",
    "com.zsy.thirdparty.srdebugger": "https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.thirdparty.srdebugger"
  }
}
```

> Demo 的另一种导入方式：不引入独立包，直接在 Package Manager 选中 ZSY Framework → Samples 页 → Import（内容与独立包一致）。

### 锁定版本

任意一行的 URL 末尾追加 `#v0.2.0`（tag）或 `#<commit>`：

```
https://github.com/git736995675/SYFrameWorkProject.git?path=Packages/com.zsy.framework#v0.2.0
```

### 升级 / 私有仓库

- 升级：改 manifest 里的 `#` 版本号，或删 `Library/PackageCache/com.zsy.*@<hash>` 缓存后重新解析
- 私有仓库：配 SSH key（URL 换 `git@github.com:...`）或 PAT（`https://<TOKEN>@github.com/...`）

## 框架版本发布流程（管理工程侧）

1. 修改对应包的 `package.json` 中的 `version`
2. 提交并 push
3. 打 tag：

```bash
git tag v0.2.0
git push origin main --tags
```

## 维护操作手册

### 新增一个第三方插件（如 SuperTween）

1. 拷入 `Packages/com.zsy.thirdparty.supertween/`，写 `package.json`（name/displayName/version/unity）
2. 确认插件有 asmdef；没有就补一对（Runtime + Editor 成对，Editor 引用 Runtime）
3. 在本 README 的配套包表格和框架 `package.json` 的 description 里各加一行
4. `git add -A && git commit && git push`

> 插件目录名不要以 `~` 结尾（会被全局 gitignore 的 `*~` 规则误伤）。

### 修改 Demo

Demo 的权威源是 `Samples~/Demo`（框架包内）与 `com.zsy.framework.demo/`（独立包），**两处内容需保持同步**：

- 改动 Demo 时先改 `Samples~/Demo`，再用同步命令刷新独立包（或反之），一起提交
- 在应用项目 Import 出来的 `Assets/Samples/...` 是复制体，改动不会回流；值得沉淀的改动拷回管理工程

### 发布新版本

```bash
git add -A && git commit -m "更新内容"   # 1. 提交
# 2. package.json version 升号
git tag v0.2.0                            # 3. 打 tag
git push origin main --tags               # 4. 推送
```

## 新插件引入规则

往框架里加第三方插件前先问：**框架代码用到它吗？**

- 用到 → 放框架包本体（目前：UniRx、DOTween）
- 用不到 → 建独立 `com.zsy.thirdparty.<name>/` 包
- 插件必须自带或补上 asmdef（Runtime + Editor 成对），避免被罩进自动程序集
