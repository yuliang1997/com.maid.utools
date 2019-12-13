# UTools

一个工具包的集合，包括一些使用unity工作的实用工具
- [ReferenceTool](#referencetool)
- [BookmarkTool](#bookmarktool)
- [FindStringTool](#findstringtool)

### 依赖
- Unity 2019.1或者更高
- [ripgrep](https://github.com/BurntSushi/ripgrep) (只有ReferenceTool需要，并且已经自带Windows版本，如果你是其他系统，需要自己安装ripgrep，并且放到 **/usr/local/bin/rg** 目录)
- [UTools.Utility](https://github.com/yuliang1997/UTools.Utility) 一些实用性的工具函数和扩展函数

### 使用方法

```json
{
  "dependencies": {
    "com.maid.utools.utility": "https://github.com/yuliang1997/UTools.Utility.git",
    "com.maid.utools": "https://github.com/yuliang1997/UTools.git",
    ...
  },
}
```
1. 在你的 **{unity项目目录}/Packages/manifest.json** 文件里插入上述两行
2. 回到Unity，等待进度条结束
3. unity菜单栏Windows/UTools/...
-----------

# ReferenceTool
查找引用工具，基于Unity资源GUID实现

![alt](https://raw.githubusercontent.com/yuliang1997/images/master/Snipaste_2019-11-29_19-03-03.png)

#### 使用
1. 点击右下角的GenerateGUIDMap按钮来建立索引，这一步比较耗时，与Unity项目大小成正比，如果Unity项目存放在SSD里，那建立速度很快，8G左右的测试项目大概需要5s
2. 选择要查询的类型:窗口顶部切页 
    - Assets(查找项目里的资源)
    - String(查找特定字符串)
    - BuiltinComponent(查找内置的脚本引用)
3. 设置查找目标:顶部第二行左边的切页
    - Selection(查找当前选中的资源)
    - Field(查找下面的FindTarget引用的资源)
4. 设置什么时候查找:顶部第二行右边的切页
    - ClickFind(当点击下面的Find按钮的时候)
    - SelectionChange(当前选中资源变更的时候)

你可以把在 **{unity项目目录}/Packages/Data/UTools** 目录下的 **guidMap.json** 文件提交到你的vcs(svn、git)上，这样其他人就可以不用生成索引就能查找项目里已有资源的引用了

这个工具只会从已生成的索引里查找引用，所以如果在生成GUIDMap之后做的修改是查找不到的，如果需要查找本地的修改结果，需要使用左下角的 [Sync Change](#syncchange) 按钮
(当然你也可以用Generate GUIDMap来重新建立全部索引)

#### SyncChange
每次当你保存修改的资源的时候(修改、添加、删除、移动)这个工具都会在 **{你的unity项目目录}/Packages/Data/UTools/assetChangeLogPath.txt** 这个文件的尾部追加上资源修改的记录，当你点击Sync Change按钮的时候，就会根据这个记录来重新建立这些涉及的资源的索引

-----------

# BookmarkTool
书签/历史工具，可以记录资源选中历史以及打开历史或者定义常用目录的快捷方式

![alt](https://raw.githubusercontent.com/yuliang1997/images/master/Snipaste_2019-11-29_20-24-22.png)

#### 使用
窗口上半部分会显示你最近的资源选择历史，下半部分是你自定义的目录标签，你可以从Unity的Project窗口拖动一个文件夹到底下的灰色拖动区域来添加一个快捷书签，当点击右边的按钮的时候Project窗口会高亮这个目录，如果你把某个书签左边的输入框内的路径删除，这个书签就会被删除

-----------

# FindStringTool
字符串查找工具，查找场景内所有匹配的字符串
这个没啥好说的。。