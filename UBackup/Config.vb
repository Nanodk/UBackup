Imports System.IO

''' <summary>
''' Store some configuration information
''' </summary>
Public Class Config

#Region "Constantes"
    Const LOGNAME As String = "[CONFIG] "
#End Region

#Region "Properties"
    Public Shared FileConfig As String = Path.Combine(Utilities.ExecutableFolder, "settings.conf")

    Public Shared EncryptionKey As Byte() = Nothing
    Public Shared IgnoreFilesizeMin As Integer = 0
    Public Shared IgnoreExt As String = vbNullString
    Public Shared NewsGroupServer As String = vbNullString
    Public Shared NewsGroupPort As String = vbNullString
    Public Shared NewsGroupUser As String = vbNullString
    Public Shared NewsGroupPassword As String = vbNullString
    Public Shared NewsGroupRetention As Integer = 365 * 3
    Public Shared NewsGroupConns As Integer = 20
    Public Shared UploadFileSuffix As String = vbNullString
    Public Shared UploadGroups As String = "alt.binaries.backup"
    Public Shared UploadArticleSize As Integer = 700

    Public Shared UploadPoster As String = "Poster <ano@nymous>"
    Public Shared Folders As New List(Of String)
#End Region

#Region "Load"
    ''' <summary>
    ''' Load configuration file
    ''' </summary>
    Public Shared Function Load() As Boolean
        Try
            If File.Exists(FileConfig) = False Then
                Log.I(LOGNAME & "file not found")
                Return False
            End If

            Dim lines() As String = File.ReadAllLines(FileConfig)
            For Each l As String In lines
                If String.IsNullOrEmpty(l) Then
                    Continue For
                End If
                l = l.Trim()
                If l.StartsWith("#") Then
                    Continue For
                End If
                Dim tmp() As String = l.Split("="c)
                If tmp.Length = 2 Then
                    Select Case tmp(0).ToUpper()
                        Case "ENCRYPTION_KEY" : EncryptionKey = Utilities.UTF8.GetBytes(Utilities.Checksum(Utilities.UTF8.GetBytes(tmp(1))))
                        Case "IGNORE_EXT" : IgnoreExt = tmp(1).ToLower()
                        Case "IGNORE_FILESIZE_MIN" : IgnoreFilesizeMin = CInt(tmp(1))
                        Case "NEWSGROUP_SERVER" : NewsGroupServer = tmp(1)
                        Case "NEWSGROUP_PORT" : NewsGroupPort = tmp(1)
                        Case "NEWSGROUP_USER" : NewsGroupUser = tmp(1)
                        Case "NEWSGROUP_PASSWORD" : NewsGroupPassword = tmp(1)
                        Case "NEWSGROUP_RETENTION" : NewsGroupRetention = CInt(tmp(1))
                        Case "NEWSGROUP_CONNECTIONS" : NewsGroupConns = CInt(tmp(1))
                        Case "UPLOAD_ARTICLE_SIZE" : UploadArticleSize = CInt(tmp(1))
                        Case "UPLOAD_FILE_SUFFIX" : UploadFileSuffix = tmp(1)
                        Case "UPLOAD_GROUPS" : UploadGroups = tmp(1)
                        Case "UPLOAD_POSTER" : UploadPoster = tmp(1)
                        Case "UPLOAD_POSTER_RANDOM" : UploadPoster = vbNullString
                        Case "FOLDER" : Folders.Add(tmp(1).TrimEnd({"/"c,"\"c}))
                    End Select
                End If
            Next

            If String.IsNullOrEmpty(NewsGroupServer) OrElse String.IsNullOrEmpty(NewsGroupPort) OrElse String.IsNullOrEmpty(NewsGroupUser) OrElse String.IsNullOrEmpty(NewsGroupPassword) Then
                Log.E(LOGNAME & "Bad parameters")
                Return False
            ElseIf EncryptionKey Is Nothing OrElse EncryptionKey.Length = 0 Then
                Log.E(LOGNAME & "Encryption Key is mandatory")
                Return False
            End If


        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return True
    End Function
#End Region
End Class

