using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UtilityClassLibrary
{
    public class UtilityStatics
    {
        public static JsonDocumentOptions GeneralJsonDocOptions = new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    }
}
