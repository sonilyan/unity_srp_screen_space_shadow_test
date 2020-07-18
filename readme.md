2018.3.2f1 macos metal测试通过。
metal的透视投影矩阵定义和dx11一样，都是使用了反转的z定义，既[1,0]

调试过程中最大的教训就是vertex到fragment的寄存器插值方式是不一样的。这个debug了好久确认不是矩阵转换有问题后，才定位了问题。
<img src="https://raw.githubusercontent.com/sonilyan/unity_srp_screen_space_shadow_test/master/image.png" alt="test">
