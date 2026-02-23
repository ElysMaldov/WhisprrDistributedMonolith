namespace Whisprr.BlueskyService.Enums;

public enum PostSortOrder
{
  Top,
  Latest
}

public static class SortOrderExtensions
{
  public static string ToApiString(this PostSortOrder sort) => sort switch
  {
    PostSortOrder.Top => "top",
    PostSortOrder.Latest => "latest",
    _ => throw new ArgumentOutOfRangeException(nameof(sort), "Sort order does not exits")
  };
}