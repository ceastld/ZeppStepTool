# StepNumberChange(SNC)

2026.10.27更新：登录改为 Zepp 现代接口 `api-user.zepp.com`（JSON 返回 `access`，避免旧 `api-user.huami.com` Location 跳转触发 HTTP 429）。账号和密码只从环境变量 `ZEPP_USER` / `ZEPP_PASS` 读取，**不要把密码、真实邮箱或 token 写进仓库或脚本**。默认步数区间 100～400，可用 `ZEPP_STEP_MIN` / `ZEPP_STEP_MAX` 覆盖；同一天多次运行会在上次步数上累加，再夹回该区间。提交内容读同目录 `data.txt`。`run.sh` 会 source 同目录 `env` 后执行脚本，并用 qq-notify 通知。生产部署目录是 `/opt/zepp-step/`，cron 示例：`0 20 * * * /opt/zepp-step/run.sh`。

2024.10.12更新，修改淘宝时间戳报错问题，目前代码已可以正常使用

2023.03.05更新，手机号登录方式失效，可以直接用邮箱登录，支付宝微信都可以修改，在账号处填写邮箱即可，代码已修改

## 🚶‍♂️项目简介(Project Introduction)：

本项目通过 bushu.py 修改小米运动 / Zepp 步数，再同步到微信、支付宝。凭据放在部署机的 env 文件里（不要提交）。默认同目录 `run.sh` 每日定时跑一次，步数在 100～400 之间。

下方有具体使用文档说明

<hr>

## 👀特别声明(Important Clause)：

**1、请勿将本仓库的任何内容用于商业或非法目的，否则后果自负。**

2、间接使用脚本的任何用户，包括但不限于建立VPS或在某些行为违反国家/地区法律或相关法规的情况下进行传播, 本人对于由此引起的任何隐私泄漏或其他后果概不负责。

3、如果任何单位或个人认为该项目的脚本可能涉嫌侵犯其权利，则应及时通知并提供身份证明，所有权证明，我们将在收到认证文件后删除相关脚本。

4、任何以任何方式查看此项目的人或直接或间接使用该项目的任何脚本的使用者都应仔细阅读此声明。本人保留随时更改或补充此免责声明的权利。一旦使用并复制了任何相关脚本或Script项目的规则，则视为您已接受此免责声明。  

<hr>

## 🔨使用说明(Instructions)： 
* 该函数有借鉴 [网友的博客](https://www.iloveu.top/) 中的图片，代码为(公众号：Gwen博客)提供，最后由本人写出总结的方法，并上传至github中，如有侵权，请立即联系本人删除。

【腾讯文档】SNC README https://docs.qq.com/doc/DWmFLWU1MV2NpcVNQ 将使用说明写在了腾讯文档中（图文），有需要的朋友可以点开链接进行查看，以下为文字版。

【腾讯文档】SNC阿里云使用图文教程 https://docs.qq.com/doc/DWk5rTVN1SldEdWNk   阿里云版本，写的比较简陋，2022.10.23更新可以正常使用

【腾讯文档】SNC微信或支付宝不同步教程 https://docs.qq.com/doc/DWkd6ZFRseUh5YXZi 有遇到步数没有更新的话请查看这篇文档！

**项目简介：** 本项目通过代码修改小米运动 / Zepp，再同步到微信、支付宝的运动步数（**无视操作系统，苹果安卓皆可使用，无需root权限**）。凭据只放在本机 env，默认步数 100～400。

步数区间用环境变量调整，不要改脚本里的账号：

- `ZEPP_STEP_MIN` / `ZEPP_STEP_MAX`，未设置时为 100 和 400
- 可选 `TG_BOT_TOKEN` / `TG_USER_ID`，不设置则不发 Telegram

（以下为当前部署方式。旧版腾讯云函数图文仍可作背景参考，但不要再把账号密码写进代码。）

1. 从应用商店或者浏览器下载小米运动 / Zepp，打开软件并登录，不要使用第三方账号登录

2. 点击我的->第三方接入，绑定你想同步数据的项目 注：同步微信运动请按照要求关注公众号。

3. 将 `bushu.py`、`data.txt`、`run.sh` 放在同一目录。生产目录是 `/opt/zepp-step/`

4. 在该目录创建 `env`（已被 git 忽略，**不要提交**），写入 `ZEPP_USER`、`ZEPP_PASS`，可选 `ZEPP_STEP_MIN`、`ZEPP_STEP_MAX`

5. 默认步数 100～400。同一天再次运行会累加后再夹回该区间

6. 试跑：`bash run.sh`。定时示例：`0 20 * * * /opt/zepp-step/run.sh`

7. 成功的话支付宝会显示步数

8. `run.sh` 会调用 `qq-notify` 发送结果。Telegram 仅在设置了 `TG_BOT_TOKEN` 和 `TG_USER_ID` 时发送。不要把这些值写进仓库。

### 如有任何问题，请留下您的Issues，看到都会回复，尽可能的帮你解决问题

<hr>

## ✍🏻️ 个人使用建议(Personal Proposal)：

**我本人每天刷步数主要是为了三个目的：**

1、蚂蚁森林每天296g的能量；

2、支付宝->运动->行走捐->捐步做公益；（支付宝每天可捐赠额度大约100w左右）

3、微信运动->步数排行榜->我的主页，下滑至底部->捐赠步数->一块走小程序。（一块走还可以领个小红花，小红花可以兑换一些礼品，微信每天可捐赠额度大约50w左右）

（目前微信运动捐不了了，直接点开一块走小程序即可）

**能够用代码来为世界做一些善事，我认为是一件很有意义的事情，代码让我感觉到我对这个世界有那么一丝丝贡献，希望大家也可以用代码为世界多做一些贡献。**

**“希望还有机会重新认识这个世界，不辜负这一生吃过的苦。最后如果还能做出点让别人生活更美好的事，那这辈子就赚了。”
（中科院2017届毕业博士黄国平的论文致谢）**

<hr>

**最后，送各位一句话。勿以善小而不为，勿以恶小而为之。**

___Don't to small and not for good, it is a sin to steal a pin___

<hr>

### 📷截图(Screenshots)

<hr>
<blockquote>
<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/step0414.png" width="35%">  
<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/step260502.jpg" width="30%">
<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/step260503.jpg" width="30%">


<div align=center>

<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/微信截图_20220612142253.png" width="30%">  
<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/sc0612.png" width="30%">
  <img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/redflower.png" width="35%"> 

上图为腾讯公益中可以兑换的礼品，每日限量，需要每日早上9点抢购一下，算一些小羊毛吧，本人亲测可以兑换到袋子（单日限量50）【2022.10最新更新，已失效，无法兑换了】
</div>
  </blockquote>
<hr>

<blockquote>
  
  <h2>❤感谢以下作者：</h2>
  
   * [网友博客提供的图片](https://www.iloveu.top/)
  
  * 微信公众号：Gwen博客
  
  * Thanks a lot
 </blockquote>
 
 ### 🌟Star 历史(Star History)
 
 <hr>
 
[![Star History Chart](https://api.star-history.com/svg?repos=EEEugene/StepNumberChangeSNC&type=Date)](https://star-history.com/#bytebase/star-history&Date)
 
 <hr>
 
 ### 支持(Support)

<div align=center>

<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/zfb.jpg" width="30%">
<img src="https://sszblog.oss-cn-shenzhen.aliyuncs.com/img/wx06.png" width="30%">
</div>
 
 <h1 align="center">点个star再走吧🌟🌟🌟</h1>
