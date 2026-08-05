<div align="center">

<img src="https://raw.githubusercontent.com/ZhangSir9901/Chrome_Portable_Updater/main/logo.ico" width="100" alt="Logo" onerror="this.src='https://cdn.jsdelivr.net/gh/twitter/twemoji@14.0.2/assets/72x72/1f680.png'">

<h1>Chrome 智能升级器 · Portable Updater</h1>

**专为受网络审查限制的用户与极致硬核玩家量身打造的 Chrome 便携版管理神器**

不仅致力于突破网络封锁，实现官方原版的满速直连更新；更提供高度自由的可视化外置配置引擎，全域封杀流氓遥测与臃肿 AI，还你绝对纯净的浏览体验。

<p align="center">
  <img src="https://img.shields.io/github/stars/ZhangSir9901/Chrome_Portable_Updater?style=flat-square&color=3498db&logo=github" alt="stars">
  <img src="https://img.shields.io/github/release/ZhangSir9901/Chrome_Portable_Updater?style=flat-square&color=1abc9c" alt="release">
  <img src="https://img.shields.io/github/issues/ZhangSir9901/Chrome_Portable_Updater?style=flat-square&color=f1c40f" alt="issues">
  <img src="https://img.shields.io/badge/arch-amd64%20%7C%20x86-lightgrey?style=flat-square" alt="arch">
  <img src="https://img.shields.io/badge/OS-Win7_11-success?style=flat-square" alt="os">
</p>

<p align="center">
  <a href="https://t.me/YuC2027" target="_blank"><img src="https://img.shields.io/badge/Telegram-@YuC2027-2CA5E0?style=flat-square&logo=telegram&logoColor=white" alt="Telegram"></a>
</p>

<h3>
  <a href="#-使用说明">快速开始</a> · 
  <a href="#-核心功能一览">核心特性</a> · 
  <a href="#%EF%B8%8F-2-极客首选外部优化引擎与可视化面板">极客设置</a> · 
  <a href="#-意见反馈与技术支持">技术支持</a> · 
  <a href="#-赞助与支持">赞助打赏</a>
</h3>

<br>

</div>

> 💡 **蓝奏云直链下载**：http://wwbba.zizi2.com/b0rb9mlmd （密码: `2jfn`）

---

## ✨ 核心功能一览

### 🌐 1. 突破封锁：便携版极速下载与无感更新
*   **免代理直连下载**：自动匹配官方最新版本，国内网络满速直连下载，彻底告别卡顿与更新失败，无视网络审查。
*   **多通道自由切换**：支持 **稳定版 (Stable)**、**测试版 (Beta)**、**开发版 (Dev)**、**金丝雀版 (Canary)** 四大分支一键切换。
*   **自身无感自更新**：启动时自动检测升级器自身新版本，发现新版以优雅的呼吸灯提醒，点击后自动无感影子热替换。

### ⚙️ 2. 极客首选：外部优化引擎与可视化面板
*   **JSON 引擎外置化**：所有的优化规则全部剥离至同目录的 `optimizer_config.json` 中。支持免编译直接修改，实现各种规则的“热拔插”。
*   **可视化极客设置**：内置极其优雅的 UI 设置面板。支持一键控制：开启内存节省模式、屏蔽推销标签页、隐藏应用快捷方式、禁用 HTTPS 强制警告等 15+ 项底层配置。
*   **零启动参数污染**：抛弃传统便携版带有一长串丑陋启动参数的做法，通过向底层注入规则，保持快捷方式的极致纯净。

### 🔗 3. 全局接管：系统默认注册与外部链接唤醒
*   **全系统默认接管**：完美兼容 Win7 至 Win11，一键将便携版注册为系统默认浏览器。
*   **外部链接新标签页打开**：彻底解决便携版浏览器在微信、QQ、Word 等外部软件点击链接时会“新开独立窗口”的痛点，通过底层 `UserDataDir` 组策略重定向，实现**100% 在当前窗口新建标签页**。

### 🤖 4. AI 净化：双向体检与彻底物理清除
*   **双向扫描清理**：一键智能扫描便携目录与系统 C 盘原版 Chrome 目录，清空静默下载的本地大模型（Gemini Nano）缓存，瞬间释放数 GB 系统盘空间。
*   **全域 AI 功能封印**：通过强注企业级组策略，彻底阉割标签栏“问问 Gemini”、AI 辅助撰写、标签页自动整理及后台静默下载通道，还你纯净的浏览体验。

### 🧹 5. 性能榨取：缓存精准控制与遥测拦截
*   **开机静默瘦身**：每次启动升级器时，若检测到 Chrome 未运行，将自动在后台无感清空系统与便携版的临时无用缓存。
*   **自定义缓存上限**：支持设置浏览器磁盘缓存与音视频媒体缓存上限（超额自动循环清理），从源头防止磁盘空间无故膨胀。
*   **遥测与后台拦截**：一键阻断 Chrome 崩溃日志上传、禁止静默预加载网页、禁止组件后台私自更新，极致榨取系统性能与网络带宽。

---

## 🛠️ 使用说明

1. **下载即用**：下载 `ChromeUpdater.exe` 后直接双击运行（绿色单文件，无额外 DLL 依赖，绝不向系统盘越权写入自身配置）。
2. **常规升级**：点击【检查更新】即可自动下载解压最新 Chrome 便携版；点击【启动浏览器】即可秒开运行。
3. **极客定制**：点击【⚙️ 极客设置】即可自定义浏览器的硬核性能控制与 UI 纯净度规则，所有更改将在下次启动浏览器时通过底层接管生效。

---

## 💬 意见反馈与技术支持

软件在持续优化迭代中，如果您在使用过程中遇到了任何问题、发现 Bug 或有更好的功能建议，**非常欢迎与我联系或提交反馈！** 您的每一个建议都是本工具不断完善的最大动力。

*   <img src="https://cdn.simpleicons.org/wechat/07C160" width="16" align="center" /> **微信咨询**：`jiujiujiayi666` (点击程序界面上的微信图标可直接复制)
*   <img src="https://cdn.simpleicons.org/telegram/26A5E4" width="16" align="center" /> **Telegram**：[@YuC2027](https://t.me/YuC2027)
*   <img src="https://cdn.simpleicons.org/googlechrome/4285F4" width="16" align="center" /> **官方网站**：[乾旭网络](https://www.qianxu.vip)
*   <img src="https://cdn.simpleicons.org/github/181717" width="16" align="center" /> **提交 Issue**：欢迎在 GitHub 提交 [Issues](https://github.com/ZhangSir9901/Chrome_Portable_Updater/issues) 交流探讨。

---

## ☕ 赞助与支持

如果这个小工具为你节省了宝贵的时间或释放了磁盘空间，欢迎赞助打赏，感谢你的支持与鼓励！

### <img src="https://cdn.simpleicons.org/alipay/1677FF" width="20" align="center" /> 支付宝 (Alipay)
```text
pdlr@qq.com
```

### <img src="https://cdn.simpleicons.org/paypal/00457C" width="20" align="center" /> PayPal
```text
paypal.me/9901Ccxm
```

### <img src="https://cdn.simpleicons.org/tether/26A17B" width="20" align="center" /> USDT (TRC20)
```text
TXS6K4jaomQn26QsouSkdUZPDRo8Rd63zj
```

---

## 👥 贡献者 (Contributors)

感谢所有参与反馈和探讨的朋友们！

<a href="https://github.com/ZhangSir9901/Chrome_Portable_Updater/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=ZhangSir9901/Chrome_Portable_Updater" />
</a>

## 📈 关注曲线 (Star History)

[![Star History Chart](https://api.star-history.com/svg?repos=ZhangSir9901/Chrome_Portable_Updater&type=Date)](https://star-history.com/#ZhangSir9901/Chrome_Portable_Updater&Date)

<br>
<div align="center">
  <i>"Talk is cheap. Show me the code." —— 保持热爱，奔赴山海。欢迎大家下载体验并提出宝贵意见！</i>
</div>