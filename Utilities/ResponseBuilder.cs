using FusionComms.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace FusionComms.Utilities
{
    public static class Util
    {
        public static GlobalResponse<T> BuildResponse<T>(int status, string statusText, ModelStateDictionary errs, T data)
        {
            var listOfErrorItems = new HashSet<ErrorItemModel>();
            var benchMark = new List<string>();

            if (errs != null)
            {
                foreach (var err in errs)
                {
                    ///err.error.errors
                    var key = err.Key;
                    var errValues = err.Value;
                    var errList = new List<string>();
                    foreach (var errItem in errValues.Errors)
                    {
                        errList.Add(errItem.ErrorMessage);
                        if (!benchMark.Contains(key))
                        {
                            listOfErrorItems.Add(new ErrorItemModel { Key = key, ErrorMessages = errList });
                            benchMark.Add(key);
                        }
                    }
                }
            }

            var res = new GlobalResponse<T>
            {
                Status = status,
                StatusText = statusText,
                Data = data,
                Errors = listOfErrorItems
            };

            return res;
        }
    }
}
