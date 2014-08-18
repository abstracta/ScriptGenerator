namespace Abstracta.FiddlerSessionComparer
{
    public enum ComparerResultType
    {
        /// <summary>
        /// ShowAll refers to all parameters, equals and differents, including NULL ones.
        /// HideEquals refers the parameters that are not equals: differents and NULL ones.
        /// HideNullOrEquals refers only to differents paramenters, but not NULL ones.
        /// </summary>
        ShowAll,
        HideEquals,
        HideNullOrEquals
    }
}