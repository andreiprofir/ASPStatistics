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

        public static string GetCssClassForBackGround(int? threadNumber)
        {
            switch (threadNumber)
            {
                case 0:
                    return "bg-success";
                case 1:
                    return "bg-danger";
                case 2:
                    return "bg-warning";
                case 3:
                    return "bg-primary";
                default:
                    return "bg-dark";
            }
        }

        public static string GetCssClassForTextColor(int? threadNumber)
        {
            switch (threadNumber)
            {
                case 0:
                    return "text-success";
                case 1:
                    return "text-danger";
                case 2:
                    return "text-warning";
                case 3:
                    return "text-primary";
                default:
                    return "text-dark";
            }
        }
    }
}