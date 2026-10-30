using System;

namespace ZSYFramework.UI
{
/// <summary>
/// 视图参数。打开视图时携带的上下文数据。
/// </summary>
public class ViewArgs
{
public Action OpenEvent { get; set; }
public Action CloseEvent { get; set; }
}
}
