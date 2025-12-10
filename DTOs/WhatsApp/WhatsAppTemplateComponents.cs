using System.Collections.Generic;

namespace FusionComms.DTOs.WhatsApp
{

    #region Interfaces

    public interface ITemplateComponent
    {
        public string type { get; set; }
    }

    public interface IButton
    {
        public string type { get; set; }
    }

    public interface IButtonComponent
    {
        public string type { get; set; }
    }

    #endregion


    #region Base and Utility Classes

    public class TemplateVariableExample
    {
        public List<string[]> body_text { get; set; } = new List<string[]>();
    }

    public class BaseTextComponent : ITemplateComponent
    {
        public string type { get; set; }
        public string text { get; set; }
    }

    #endregion


    #region Template Component Classes (API SENDING PAYLOAD)

    public class BodyComponent : ITemplateComponent
    {
        public string type { get; set; }
        public List<BaseTextComponent> parameters { get; set; }

        public BodyComponent(List<BaseTextComponent> arguments)
        {
            type = "body";
            parameters = arguments;
        }
    }

    public class HeaderComponent : ITemplateComponent
    {
        public string type { get; set; } = "HEADER";
        public string format { get; set; }
        public string text { get; set; }
    }

    public class ButtonComponent : ITemplateComponent, IButton
    {
        public string type { get; set; } = "button";
        public string sub_type { get; set; }
        public string index { get; set; }
        public List<IButtonComponent> parameters { get; set; }

        public ButtonComponent(string subtype, List<IButtonComponent> parameterz, string i_ndex = "")
        {
            sub_type = subtype;
            index = i_ndex;
            parameters = parameterz;
        }
    }

    #endregion



    #region Template Creation Component Classes (API CREATION PAYLOAD)

    public class CreateBodyComponent : BaseTextComponent
    {
        public TemplateVariableExample example { get; set; }

        public CreateBodyComponent(string bodyText, List<string> bodyExamples)
        {
            type = "BODY";
            text = bodyText;
            example = new TemplateVariableExample
            {
                body_text = new List<string[]>
                {
                    bodyExamples.ToArray()
                }
            };
        }
    }


    public class FooterComponent : BaseTextComponent
    {
        public FooterComponent(string footerText)
        {
            type = "FOOTER";
            text = footerText;
        }
    }

    public class ButtonsComponent : ITemplateComponent
    {
        public string type { get; set; } = "BUTTONS";
        public List<IButton> buttons { get; set; } = new List<IButton>();
    }

    #endregion



    #region Button Parameter Classes (IButtonComponent)

    public class CopyCodeButtonComponenet : IButtonComponent
    {
        public string coupon_code { get; set; }
        public string type { get; set; }
    }

    #endregion



    #region Button Classes (IButton)

    public class UrlButton : IButton
    {
        public string type { get; set; } = "URL";
        public string text { get; set; }
        public string url { get; set; }
    }

    public class QuickReplyButton : IButton
    {
        public string type { get; set; } = "QUICK_REPLY";
        public string text { get; set; }
    }

    public class CreateCopyCodeButton : IButton
    {
        public string type { get; set; } = "COPY_CODE";
        public string example { get; set; }
    }

    #endregion
}