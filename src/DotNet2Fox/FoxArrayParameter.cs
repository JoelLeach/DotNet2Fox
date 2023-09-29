using System.Runtime.InteropServices;

namespace DotNet2Fox
{
    /// <summary>
    /// VFP doesn't support array within array, and parameters have already been passed as array.
    /// To pass array, it must first be wrapped with object: new FoxArrayParameter(myDotNetArray).
    /// To access array in VFP, get Array property: loArrayParameter.Array.
    /// </summary>
    [ComVisible(true)]
    public class FoxArrayParameter
    {
        /// <summary>
        /// VFP doesn't support array within array, and parameters have already been passed as array.
        /// To pass array, it must first be wrapped with object: new FoxArrayParameter(myDotNetArray).
        /// To access array in VFP, get Array property: loArrayParameter.Array.
        /// </summary>
        /// <param name="array">DotNet array that will be passed to VFP as parameter.</param>
        public FoxArrayParameter(object array)
        {
            Array = array;
        }

        /// <summary>
        /// To access array in VFP, get Array property: loArrayParameter.Array.
        /// </summary>
        public object Array { get; }
    }
}
