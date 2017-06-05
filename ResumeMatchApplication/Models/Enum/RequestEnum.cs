using System.ComponentModel;

namespace ResumeMatchApplication.Models
{
    public enum RequestEnum
    {
        POST, GET
    }

    public enum ContentTypeEnum
    {
        [Description("application/x-www-form-urlencoded")]
        Form,
        [Description("application/json")]
        Json,
        [Description("text/html")]
        Text
    }
}