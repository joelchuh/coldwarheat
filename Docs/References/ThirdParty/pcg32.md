# PCG32来源与许可

Core/Random/Pcg32Random.cs中的PCG XSH-RR状态转换、输出与初始化，根据Melissa O’Neill的pcg-c-basic改写为C#。

Copyright 2014 Melissa O’Neill <oneill@pcg-random.org>

原始来源：https://github.com/imneme/pcg-c-basic/blob/master/pcg_basic.c

Licensed under the Apache License, Version 2.0. 完整许可见[Apache-2.0.txt](Apache-2.0.txt)，下载自https://www.apache.org/licenses/LICENSE-2.0.txt（2026-10-13）。本说明只适用于所注明的第三方派生算法代码，不替整个项目指定许可。

本项目改动：C#类型和异常、完整快照、独立Fork、原始输出计数、有界调用失败时不发布局部状态、版本化JSON。初始化将两个固定转换合并计算，初始化不计入DrawCount。

参考验证使用作者https://www.pcg-random.org/using-pcg-c-basic.html的seed42/stream54示例输出；seed来源由https://github.com/imneme/pcg-c-basic/blob/master/pcg32-demo.c确认。
