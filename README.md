# 🚀 Chrome Portable Updater (Chrome 智能升级器)

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20GPU%20Acceleration-success.svg)](#)
[![OS](https://img.shields.io/badge/OS-WinXP%20%7C%20Win7%20%7C%20Win10%20%7C%20Win11-lightgrey.svg)](#)

一款专为中国大陆网络环境深度定制的 **Chrome 便携版智能下载、升级与环境接管神器**。  
基于 WPF 打造，采用纯异步非阻塞架构与 GPU 硬件加速引擎，彻底告别假死与卡顿。无论你是追求纯净环境的极客，还是需要大规模部署的运维，它都能为你提供极度丝滑的无感体验。

> **核心哲学**：不卡死、不报毒、界面极度丝滑、免翻墙。

---

## ✨ 核心特性：四大黄金定律

### 🛡️ 1. 网络层的“三路容灾”与“直连国内节点”
*   **免翻墙极速下载**：彻底摒弃国外容易被阻断的源。版本解析直连阿里巴巴国内高速镜像源（npmmirror.com），安装包下载直达谷歌国内 CDN 节点（dl.google.com），下载速度瞬间拉满。
*   **防 403 与反拦截**：底层物理屏蔽本地代理（Proxy = null）并强制注入合法 User-Agent，无视本地 Clash/V2ray 代理干扰，彻底防止网络请求被挂死。

### 🎨 2. 界面层的“无感切换”与“GPU 硬件加速”
*   **极美模态遮罩层**：摒弃繁琐丑陋的多窗口弹窗，采用同窗口内 Overlay 叠加遮罩，优雅锁死主进程操作。
*   **60帧极致动画**：进度条彩蛋不使用定时器重绘，直接调动 WPF 原生的 Storyboard 引擎与 GPU 3D 渲染，文字滚动自带发光特效，纵享 60 帧极度丝滑。

### ⚙️ 3. 系统层的“双规注册表”与“全系统接管”
*   **跨越 XP 到 Win11 的默认浏览器接管**：
    *   *Win10/Win11*：静默写入专属 ProgID（"Google Chrome 便携版"）上系统户口，智能呼出系统设置面板并执行 30 秒无感异步轮询。
    *   *WinXP/Win7*：自动降级为暴力注入模式，瞬间强改底层关联，无需用户干预。
*   **归一化路径匹配**：底层 IO 与注册表查验严格实行绝对路径的小写正斜杠归一化，判定成功率 100%。

### ⚡ 4. 架构层的“异步非阻塞”与“单点维护”
*   **UI 线程绝对霸权**：凡是涉及网络请求、大文件 IO、解压、注册表写入等耗时操作，全部强制挂入 await Task.Run() 后台开辟。主线程只管渲染，软件永远不会出现“无响应”。
*   **版本号单点控制**：代码顶层常量控制全域，杜绝版本号不同步的低级错误。

---

## 🛠️ 编译与使用

1. 克隆本仓库到本地：
   `git clone https://github.com/ZhangSir9901/Chrome_Portable_Updater.git`

2. 使用 Visual Studio 打开解决方案。
3. 确保安装了 .NET Framework / .NET Core (WPF 开发工作负载)。
4. 直接按 F5 编译运行即可。

---

## 👨‍💻 作者与技术支持

如果你在 RPA开发、C# / WPF 桌面端开发、或者复杂的环境逆向部署中遇到难题，欢迎与我交流探讨！

*   **微信**：`jiujiujiayi666`
*   **Telegram**：[`@YuC2027`](https://t.me/YuC2027)
*   **技术支持网站**：[https://www.qianxu.vip](https://www.qianxu.vip)

> 💡 **如果您对软件有其它建议，欢迎在仓库提交 [Issues](https://github.com/ZhangSir9901/Chrome_Portable_Updater/issues)；如果有其它定制需求，请通过上方联系方式与我联系。**

---

## ☕ 赞助与打赏

如果这个项目为你节省了宝贵的时间，或者你惊叹于它极度丝滑的代码艺术，欢迎打赏支持！您的支持是我持续开源与输出硬核代码的最大动力！

**💳 支付宝 (Alipay)**
> 👇 鼠标悬停在下方代码框内，点击右上角即可 **一键复制**：

```text
pdlr@qq.com
```

**🪙 USDT (TRC20)** 
> 👇 鼠标悬停在下方代码框内，点击右上角即可 **一键复制**：

```text
TXS6K4jaomQn26QsouSkdUZPDRo8Rd63zj
```

<br>
<div align="center">
  <i>"Talk is cheap. Show me the code." —— 保持热爱，奔赴山海。</i>
</div>
