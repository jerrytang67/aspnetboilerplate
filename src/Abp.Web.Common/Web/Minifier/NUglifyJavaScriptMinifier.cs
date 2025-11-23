using Abp.Logging;
using NUglify;
using Microsoft.Extensions.Logging;

namespace Abp.Web.Minifier {
    public class NUglifyJavaScriptMinifier(ILogger<NUglifyJavaScriptMinifier> logger) : IJavaScriptMinifier {
        public string Minify(string javaScriptCode) {
            Check.NotNull(javaScriptCode, nameof(javaScriptCode));

            var result = Uglify.Js(javaScriptCode);
            if (!result.HasErrors) {
                return result.Code;
            }

            logger.LogWarning($"{nameof(NUglifyJavaScriptMinifier)} has encountered an error in handling javascript.");
            result.Errors.ForEach(error => logger.LogWarning(error.ToString()));

            return javaScriptCode;
        }
    }
}