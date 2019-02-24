Imports System.IO

Module Module1

    Const LOGNAME As String = "[MAIN] "

    Sub Main(args As String())
        Call ProcessWrapper.KillProcesses()

        Console.WriteLine("******************")
        Console.WriteLine("*   UBackup      *")
        Console.WriteLine("*   www.zem.fr   *")
        Console.WriteLine("******************")

        If Config.Load() = False Then
            Log.E(LOGNAME & "Error loading """ & Config.FileConfig & """")
            Exit Sub
        End If

        If EnsureAllDirectoriesAndExe() = False Then
            Log.E(LOGNAME & "Error creating dirs")
            Exit Sub
        End If

        If Database.Load() = False Then
            Log.E(LOGNAME & "Error loading """ & Database.FileDatabase & """")
            Exit Sub
        End If

        Call Log.Start()
        Console.WriteLine("*******************")

        Console.Title = "UBackup (ENTER to quit) - MENU"
        If args IsNot Nothing AndAlso args.Length > 0 Then
            Select Case args(0).ToLower()
                Case "-b"
                    Call CmdBackup()
                Case "-r"
                    If args.Length > 1 Then
                        Call CmdRestore(args(1))
                    Else
                        Call DisplayHelpCmdArgs()
                    End If
                Case "-z"
                    Call CmdZip()
                Case "-t"
                    Call CmdTree()
                Case Else
                    Call DisplayHelpCmdArgs()
            End Select

        Else
            Call DisplayHelp()

            Dim cmd As String = Console.ReadLine()
            If cmd = "1" Then
                Call CmdBackup()
            ElseIf cmd = "2" Then
                Console.WriteLine("*******************")
                Console.WriteLine("* RESTORE MODE    *")
                Console.WriteLine("*******************")
                Console.WriteLine("Please enter entry path (folder) to restore: ")
                Dim entryPath As String = Console.ReadLine()
                Call CmdRestore(entryPath)
            ElseIf cmd = "3" Then
                Call CmdZip()
            ElseIf cmd = "4" Then
                Call CmdTree()
            Else
                Console.WriteLine("Unknown command - ENTER to quit")
            End If
            Console.ReadLine()
        End If

        'Console.ReadLine()
        'Call StopAll()
    End Sub

#Region "Help"
    ''' <summary>
    ''' Display CmdLine Help
    ''' </summary>
    Private Sub DisplayHelpCmdArgs()
        Console.WriteLine("Available command args: ")
        Console.WriteLine("-b: Backup")
        Console.WriteLine("-r ""folder_path"": Restore")
        Console.WriteLine("-z: Zip nzb folder")
        Console.WriteLine("-t: Tree database")
    End Sub

    ''' <summary>
    ''' Display Help
    ''' </summary>
    Private Sub DisplayHelp()
        Console.WriteLine("Please select action: ")
        Console.WriteLine("1: Backup")
        Console.WriteLine("2: Restore")
        Console.WriteLine("3: Zip nzb folder")
        Console.WriteLine("4: Tree database")
    End Sub
#End Region

#Region "Cmd"
    ''' <summary>
    ''' Restore command
    ''' </summary>
    Private Sub CmdRestore(restorePath As String)
        Try
            If String.IsNullOrEmpty(restorePath) Then
                Log.E(LOGNAME & "Error restore path invalid")
                Exit Sub
            End If
            If Database.Restore(restorePath) = False Then
                Log.E(LOGNAME & "Error restoring from database")
                Exit Sub
            End If
            Call Restore.Start()
        Catch ex As Exception
            Log.E(LOGNAME & "Error in command Restore")
        End Try
    End Sub

    ''' <summary>
    ''' Backup command
    ''' </summary>
    Private Sub CmdBackup()
        Try
            Console.WriteLine("*******************")
            Console.WriteLine("* BACKUP MODE     *")
            Console.WriteLine("*******************")
            If Database.Scan() = False Then
                Log.E(LOGNAME & "Error scanning database")
                Exit Sub
            End If
            If Database.Save() = False Then
                Log.E(LOGNAME & "Error saving database")
                Exit Sub
            End If
            Call Backup.Start()
        Catch ex As Exception
            Log.E(LOGNAME & "Error in command Backup")
        End Try
    End Sub

    ''' <summary>
    ''' Zip command
    ''' </summary>
    Private Sub CmdZip()
        Try
            Console.WriteLine("*******************")
            Console.WriteLine("* ZIP MODE        *")
            Console.WriteLine("*******************")
            If Database.Zip() = False Then
                Log.E(LOGNAME & "Error zipping Nzb folder")
            End If
        Catch ex As Exception
            Log.E(LOGNAME & "Error in command Zip")
        End Try
    End Sub

    ''' <summary>
    ''' Tree command
    ''' </summary>
    Private Sub CmdTree()
        Try
            Console.WriteLine("*******************")
            Console.WriteLine("* TREE MODE       *")
            Console.WriteLine("*******************")
            If Database.Tree() = False Then
                Log.E(LOGNAME & "Error generating database tree")
            End If
        Catch ex As Exception
            Log.E(LOGNAME & "Error in command Tree")
        End Try
    End Sub
#End Region

    ''' <summary>
    ''' Stop all threaded workers
    ''' </summary>
    Private Sub StopAll()
        Try
            Call Backup.Stop()
            Call Restore.Stop()
            Call Log.Stop()

            Process.GetCurrentProcess().Kill()
        Catch ex As Exception
            'nothing to do
        End Try
    End Sub

    ''' <summary>
    ''' Ensure all directories exists
    ''' </summary>
    Private Function EnsureAllDirectoriesAndExe() As Boolean
        Try
            Call Utilities.DeleteDirectory(Utilities.FolderTemp)
            Call Utilities.EnsureDirectory(Utilities.FolderZip)
            Call Utilities.EnsureDirectory(Utilities.FolderRestore)
            Call Utilities.EnsureDirectory(Utilities.FolderNzb)
            Const ALPHABET As String = "0123456789abcdefghijklmnopqrstuvwxyz"
            For i As Integer = 0 To ALPHABET.Length - 1
                Dim prefix As String = ALPHABET(i)
                Dim tmp As String = Path.Combine(Utilities.FolderNzb, prefix)
                Call Utilities.EnsureDirectory(tmp)
            Next
            Call Utilities.EnsureDirectory(Utilities.FolderTemp)

            Dim par2 As String = Path.Combine(Utilities.FolderTemp, ProcessWrapper.ExePar2)
            If File.Exists(par2) = False Then
                File.Copy(Path.Combine(Utilities.ExecutableFolder, "Bin", ProcessWrapper.ExePar2), par2)
            End If
            Dim nyuu As String = Path.Combine(Utilities.FolderTemp, ProcessWrapper.ExeNyuu)
            If File.Exists(nyuu) = False Then
                File.Copy(Path.Combine(Utilities.ExecutableFolder, "Bin", ProcessWrapper.ExeNyuu), nyuu)
            End If
            Dim nzb As String = Path.Combine(Utilities.FolderTemp, ProcessWrapper.ExeNzbGet)
            If File.Exists(nzb) = False Then
                File.Copy(Path.Combine(Utilities.ExecutableFolder, "Bin", ProcessWrapper.ExeNzbGet), nzb)
            End If
            Dim nzbconf As String = Path.Combine(Utilities.FolderTemp, ProcessWrapper.ConfNzbGet)
            If File.Exists(nzbconf) Then
                Utilities.DeleteFile(nzbconf)
            End If
            Dim nzbconfStr As String = File.ReadAllText(Path.Combine(Utilities.ExecutableFolder, "Bin", ProcessWrapper.ConfNzbGet))
            nzbconfStr = Replace(nzbconfStr, "{$NEWSGROUP_SERVER}", Config.NewsGroupServer)
            nzbconfStr = Replace(nzbconfStr, "{$NEWSGROUP_PORT}", Config.NewsGroupPort)
            nzbconfStr = Replace(nzbconfStr, "{$NEWSGROUP_USER}", Config.NewsGroupUser)
            nzbconfStr = Replace(nzbconfStr, "{$NEWSGROUP_PASSWORD}", Config.NewsGroupPassword)
            nzbconfStr = Replace(nzbconfStr, "{$NEWSGROUP_CONNECTIONS}", Config.NewsGroupConns.ToString)
            File.WriteAllText(nzbconf, nzbconfStr)

            Return True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function
End Module
