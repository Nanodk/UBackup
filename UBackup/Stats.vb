''' <summary>
''' Represents some statistics
''' </summary>
Public Class Stats

#Region "Properties"
    Public Shared NbFilesSuccess As Integer = 0
    Public Shared NbFilesFail As Integer = 0
    Public Shared NbFilesTotal As Integer = 0
    Public Shared TotalSizeProcessed As Long = 0
    Public Shared TotalSize As Long = 0
    Public Shared DateStart As Date
#End Region

    ''' <summary>
    ''' Returns a stat line
    ''' </summary>
    Public Shared Function Display(title As String) As String
        Dim bps As String = Utilities.ConvertSizeToHumanReadable(CLng(TotalSizeProcessed / (Date.UtcNow - DateStart).TotalSeconds))
        Return title & " Files: " & NbFilesSuccess & " / " & NbFilesTotal & " (Fail: " & NbFilesFail & ") | Speed: " & bps & "bps | Size: " & Utilities.ConvertSizeToHumanReadable(TotalSizeProcessed) & "B / " & Utilities.ConvertSizeToHumanReadable(TotalSize) & "B"
    End Function
End Class
