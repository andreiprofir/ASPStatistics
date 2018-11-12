namespace ASP_Statistics.Utils
{
    public static class HtmlViewHelper
    {
        public static string GetCssClassForBorderBy(int? threadNumber)
        {
            switch (threadNumber)
            {
                case 0:
                    return "border-success";
                case 1:
                    return "border-danger";
                case 2:
                    return "border-warning";
                case 3:
                    return "border-primary";
                default:
                    return "border-dark";
            }
        }
    }
}