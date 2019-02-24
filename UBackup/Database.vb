
Imports System.Collections.Concurrent
Imports System.IO
Imports Newtonsoft.Json

''' <summary>
''' Database operations
''' </summary>
Public Class Database

#Region "Constantes"
    Const LOGNAME As String = "[DATABASE] "
    Public Const TITLE_ZIP As String = "DATABASE ZIP"
    Public Const TITLE_TREE As String = "DATABASE TREE"

    Public Shared FileDatabase As String = Path.Combine(Utilities.FolderNzb, "database.db")
    Private Shared FileTree As String = Path.Combine(Utilities.FolderNzb, "tree.txt")
#End Region

#Region "Properties"
    Public Shared Db As New Dictionary(Of String, DatabaseObj)(StringComparer.OrdinalIgnoreCase)
    Public Shared FilesToProcess As New ConcurrentQueue(Of DatabaseObj)

#End Region

#Region "Load / Save"
    ''' <summary>
    ''' Load database in memory
    ''' </summary>
    Public Shared Function Load() As Boolean
        Try
            If File.Exists(FileDatabase) Then
                Db = Serializer.Deserialize(Of Dictionary(Of String, DatabaseObj))(FileDatabase)
            End If
            Log.I(LOGNAME & "Loaded")
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' Save database into file
    ''' </summary>
    Public Shared Function Save(Optional skipLog As Boolean = False) As Boolean
        Try
            If Serializer.Serialize(Db, FileDatabase) Then
                If skipLog = False Then
                    Log.I(LOGNAME & "Saved")
                End If
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return True
    End Function
#End Region

#Region "Scan / Merge"
    ''' <summary>
    ''' Return the DatabaseObj if it already exists
    ''' </summary>
    Private Shared Function GetDbo(contener As Dictionary(Of String, DatabaseObj), key As String) As DatabaseObj
        Dim ret As DatabaseObj = Nothing
        Try
            If contener.ContainsKey(key) Then
                ret = contener(key)
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return ret
    End Function

    ''' <summary>
    ''' Scan a folder and fill database
    ''' </summary>
    ''' <returns></returns>
    Public Shared Function Scan() As Boolean
        Try
            Dim dRef As Date = Date.UtcNow
            Dim nbDirs As Integer = 0
            Dim nbDirsNew As Integer = 0
            Dim nbFiles As Integer = 0
            Dim nbFilesNew As Integer = 0
            Dim nbFilesIgnore As Integer = 0
            For Each f As String In Config.Folders
                Dim dbo As DatabaseObj = GetDbo(Db, f)
                If dbo Is Nothing Then
                    dbo = New DatabaseObj
                    With dbo
                        .Name = f
                        .IsDirectory = True
                        .Files = New Dictionary(Of String, DatabaseObj)(StringComparer.OrdinalIgnoreCase)
                        .Dirs = New Dictionary(Of String, DatabaseObj)(StringComparer.OrdinalIgnoreCase)
                    End With
                    Db(dbo.Name) = dbo
                End If
                If ScanRecursively(dRef, dbo, f, nbDirs, nbDirsNew, nbFiles, nbFilesNew, nbFilesIgnore) = False Then
                    Log.E(LOGNAME & "An error occured scanning " & f)
                    Exit For
                End If
            Next
            Log.I(LOGNAME & "Dirs: " & nbDirs & " - DirsNew: " & nbDirsNew & " - Files: " & nbFiles & " - FilesNew: " & nbFilesNew & " - FilesIgnored: " & nbFilesIgnore & " - FilesToBackup: " & FilesToProcess.Count)

        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' Scan a folder recursively
    ''' </summary>
    Private Shared Function ScanRecursively(dRef As Date, contener As DatabaseObj, folder As String, ByRef nbDirs As Integer, ByRef nbDirsNew As Integer, ByRef nbFiles As Integer, ByRef nbFilesNew As Integer, ByRef nbFilesIgnore As Integer) As Boolean
        Dim ret As Boolean = True
        Try
            Dim diRef As New DirectoryInfo(folder)
            If diRef.Exists = False Then
                Log.E(LOGNAME & folder & " not found")
                Return False
            End If
            Dim files() As FileInfo = diRef.GetFiles()
            For Each fi As FileInfo In files
                nbFiles += 1
                If fi.Length < Config.IgnoreFilesizeMin Then
                    nbFilesIgnore += 1
                    Continue For
                ElseIf String.IsNullOrEmpty(Config.IgnoreExt) = False AndAlso String.IsNullOrEmpty(fi.Extension) = False AndAlso Config.IgnoreExt.IndexOf(fi.Extension.ToLower()) <> -1 Then
                    nbFilesIgnore += 1
                    Continue For
                End If
                Dim key As String = fi.Name
                Dim dbo As DatabaseObj = GetDbo(contener.Files, key)
                If dbo Is Nothing Then
                    dbo = New DatabaseObj
                    With dbo
                        .Name = fi.Name
                        .IsDirectory = False
                        .Size = fi.Length
                    End With
                    contener.Files(key) = dbo
                    nbFilesNew += 1
                End If
                If String.IsNullOrEmpty(dbo.NzbGuid) OrElse dbo.NzbDate.HasValue = False OrElse (dRef - dbo.NzbDate.Value).TotalDays > Config.NewsGroupRetention Then '1095=3*365
                    dbo.TempFileOrFolderPath = fi.FullName
                    FilesToProcess.Enqueue(dbo)
                End If
            Next
            Dim dirs() As DirectoryInfo = diRef.GetDirectories()
            For Each di As DirectoryInfo In dirs
                nbDirs += 1
                Dim key As String = di.Name
                Dim dbo As DatabaseObj = GetDbo(contener.Dirs, key)
                If dbo Is Nothing Then
                    dbo = New DatabaseObj
                    With dbo
                        .Name = di.Name
                        .IsDirectory = True
                        .Files = New Dictionary(Of String, DatabaseObj)(StringComparer.OrdinalIgnoreCase)
                        .Dirs = New Dictionary(Of String, DatabaseObj)(StringComparer.OrdinalIgnoreCase)
                    End With
                    contener.Dirs(key) = dbo
                    nbDirsNew += 1
                End If
                ret = ret And ScanRecursively(dRef, dbo, di.FullName, nbDirs, nbDirsNew, nbFiles, nbFilesNew, nbFilesIgnore)
            Next
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return ret
    End Function
#End Region

#Region "Restore"
    ''' <summary>
    ''' Return all dbo to restore
    ''' </summary>
    ''' <returns></returns>
    Public Shared Function Restore(entryPath As String) As Boolean
        Try
            If entryPath.Contains(":") Then
                entryPath = Split(entryPath, ":")(1)
            End If
            If entryPath.EndsWith("\") = False
                entryPath &= "\"
            End If
            Dim dbo As DatabaseObj = Nothing
            Dim folder As String = vbNullString
            For Each kvp As KeyValuePair(Of String, DatabaseObj) In Db
                Dim tmp As String = Split(kvp.Key, ":")(1)
                If entryPath.ToLower().StartsWith(tmp.ToLower()) Then
                    dbo = kvp.Value
                    Dim tmpPath As String = entryPath.Substring(tmp.Length)
                    Dim folders As String() = Split(tmpPath, "\")

                    For Each f As String In folders
                        If String.IsNullOrEmpty(f) Then
                            Continue For
                        End If
                        If dbo.Dirs.ContainsKey(f) = False Then
                            dbo = Nothing
                            Exit For
                        End If
                        dbo = dbo.Dirs(f)
                        If String.IsNullOrEmpty(folder) Then
                            folder = f
                        Else
                            folder = Path.Combine(folder, f)
                        End If
                    Next
                End If
            Next
            If dbo Is Nothing Then
                Log.E(LOGNAME & "EntryPath not found in DB: " & entryPath)
                Return False
            End If

            'We generate all files to restore
            Dim nbDirs As Integer
            Dim nbFiles As Integer
            Dim nbFilesWithoutNzb As Integer
            If RestoreRecursively(dbo, folder, nbDirs, nbFiles, nbFilesWithoutNzb) = False Then
                Log.E(LOGNAME & "An error occured restoring " & entryPath)
                Return False
            End If
            Log.I(LOGNAME & "Dirs: " & nbDirs & " - Files: " & nbFiles & " - FilesWithoutNzb: " & nbFilesWithoutNzb)

        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' Restore a DatabaseObj recursively
    ''' </summary>
    Private Shared Function RestoreRecursively(contener As DatabaseObj, folder As String, ByRef nbDirs As Integer, ByRef nbFiles As Integer, ByRef nbFilesWithoutNzb As Integer) As Boolean
        Dim ret As Boolean = True
        Try

            For Each dbo As DatabaseObj In contener.Files.Values
                If String.IsNullOrEmpty(dbo.NzbGuid) Then
                    nbFilesWithoutNzb += 1
                    Continue For
                End If
                nbFiles += 1
                dbo.TempFileOrFolderPath = folder
                FilesToProcess.Enqueue(dbo)
            Next

            For Each dbo As DatabaseObj In contener.Dirs.Values
                nbDirs += 1
                dbo.TempFileOrFolderPath = folder
                FilesToProcess.Enqueue(dbo)
                Dim newFolder as String = dbo.Name
                If String.IsNullOrEmpty(folder)=False
                    newFolder =Path.Combine(folder, dbo.Name)
                End If
                Call RestoreRecursively(dbo, newFolder, nbDirs, nbFiles, nbFilesWithoutNzb)
            Next
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            Return False
        End Try
        Return ret
    End Function
#End Region

#Region "Zip"
    ''' <summary>
    ''' Zip folder nbz for security
    ''' </summary>
    ''' <returns></returns>
    Public Shared Function Zip() As Boolean
        Try
            If Directory.Exists(Utilities.FolderNzb) Then
                Dim dest As String = Path.Combine(Utilities.FolderZip, "nzb_" & Date.UtcNow.ToString("yyyyMMddHHmmss") & ".zip")
                Log.I(LOGNAME & "Zipping Nzb folder")
                If Zipper.Zip(Utilities.FolderNzb, dest) = True Then
                    Log.I(LOGNAME & "Zip finished and available in Nzb folder: " & dest)
                    Return True
                End If
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function
#End Region

#Region "Tree"
    ''' <summary>
    ''' Tree database into output file
    ''' </summary>
    Public Shared Function Tree() As Boolean
        Try
            Dim ret As Boolean = True
            Log.I(LOGNAME & "Tree database generating")
            Using sw As New StreamWriter(FileTree, False)
                sw.WriteLine(TreeHeader)
                ret = ret And TreeRecursive(sw, Db, 0)
            End Using
            Log.I(LOGNAME & "Tree database is available: " & FileTree)
            Return ret
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Recursive function which loop db
    ''' </summary>
    Private Shared Function TreeRecursive(sw As StreamWriter, dicoOfDbo As Dictionary(Of String, DatabaseObj), level As Integer) As Boolean
        Try
            For Each kvp As KeyValuePair(Of String, DatabaseObj) In dicoOfDbo
                Dim dbo As DatabaseObj = kvp.Value
                sw.WriteLine(TreeDbo(dbo, level))
                If dbo.IsDirectory Then
                    If TreeRecursive(sw, dbo.Dirs, level + 1) = False Then
                        Return False
                    End If
                    If TreeRecursive(sw, dbo.Files, level + 1) = False Then
                        Return False
                    End If
                End If
            Next
            Return True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Return tree header
    ''' </summary>
    Private Shared Function TreeHeader() As String
        Return Join({"Name", "Type", "Size", "NbDirs", "NbFiles", "Checksum", "Nzb", "NzbDate"}, vbTab)
    End Function

    ''' <summary>
    ''' Format output for a DatabaseObj
    ''' </summary>
    Private Shared Function TreeDbo(dbo As DatabaseObj, level As Integer) As String
        Dim str As String = vbNullString
        If dbo.IsDirectory Then
            str = Join({dbo.Name, "D", "", dbo.Dirs.Count, dbo.Files.Count, "", "", ""}, vbTab)
        Else
            Dim size As String = vbNullString
            If dbo.Size.HasValue Then
                size = Utilities.ConvertSizeToHumanReadable(dbo.Size.Value) & "B"
            End If
            Dim dateNzb As String = vbNullString
            If dbo.NzbDate.HasValue Then
                dateNzb = dbo.NzbDate.Value.ToString("yyyy-MM-dd")
            End If
            str = Join({dbo.Name, "F", size, "", "", dbo.Checksum, dbo.NzbGuid, dateNzb}, vbTab)
        End If
        Return StrDup(level, "--") & IIf(level = 0, ""," ").ToString() & str
    End Function
#End Region

#Region "Class DatabaseObj"
    ''' <summary>
    ''' Represent a Database Object
    ''' </summary>
    Public Class DatabaseObj
        <JsonProperty(PropertyName:="o")> Public Name As String
        <JsonProperty(PropertyName:="y")> Public IsDirectory As Boolean
        <JsonProperty(PropertyName:="s")> Public Size As Long?
        <JsonProperty(PropertyName:="c")> Public Checksum As String
        <JsonProperty(PropertyName:="z")> Public NzbGuid As String
        <JsonProperty(PropertyName:="t")> Public NzbDate As Date?
        <JsonProperty(PropertyName:="f")> Public Files As Dictionary(Of String, DatabaseObj)
        <JsonProperty(PropertyName:="d")> Public Dirs As Dictionary(Of String, DatabaseObj)
        <JsonIgnore> Public TempGuid As String
        <JsonIgnore> Public TempFileOrFolderPath As String
        <JsonIgnore> Public TempChecksum As String
    End Class
#End Region

End Class

