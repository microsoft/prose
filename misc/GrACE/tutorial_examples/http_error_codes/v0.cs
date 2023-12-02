using System;
using System.Collections.Generic;

public class ErrorCodes
{
    public static void Main()
    {
        try
        {
            ts = Library.Utility.Timeparser.ParseTimeSpan(input["duration"].Value);
        }
        catch (Exception ex)
        {
            info.ReportClientError("Scheme is missing");
            return;
        }
        default:
            info.ReportClientError("No such action");
    }
}