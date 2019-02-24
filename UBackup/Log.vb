Imports System.Collections.Concurrent
Imports System.IO
Imports System.Threading

''' <summary>
''' Manage logging
''' </summary>
Public Class Log

#Region "Constantes"
    Const LOGNAME As String = "[LOG] "
#End Region

#Region "Shared Properties"
    Private Shared QueueOfLogs As New ConcurrentQueue(Of Log)
    Private Shared LogFile As String = Path.Combine(Utilities.ExecutableFolder, Utilities.AssemblyName & ".log")
    Private Shared WithEvents TimerFlush As New Timers.Timer(15000)
    Private Shared MonitorObj As New Object
#End Region

#Region "Enum"
    Public Enum LogType
        DEBUG = 0
        INFO = 1
        WARN = 2
        [ERROR] = 3
    End Enum
#End Region

#Region "Properties"
    Public Timestamp As Date = Date.UtcNow
    Public Type As LogType
    Public Message As String
#End Region

#Region "Start / Stop"
    ''' <summary>
    ''' Init the flusher
    ''' </summary>
    Public Shared Sub Start()
        Log.I(LOGNAME & "Flusher started")
        Call TimerFlush.Start()
    End Sub

    ''' <summary>
    ''' Stop the flusher
    ''' </summary>
    Public Shared Sub [Stop]()
        Log.I(LOGNAME & "Flusher stopped")
        Call TimerFlush.Stop()
        Call ForceFlush()
    End Sub
#End Region


    ''' <summary>
    ''' Flush in file
    ''' </summary>
    Private Shared Sub TimerFlushElapsed() Handles TimerFlush.Elapsed
        Call ForceFlush()
    End Sub

    ''' <summary>
    ''' Export Log Object to line
    ''' </summary>
    Private Shared Function ToLine(l As Log) As String
        Return l.Type.ToString & vbTab & l.Timestamp.ToString("o") & vbTab & l.Message
    End Function

    ''' <summary>
    ''' Add a log in queue
    ''' </summary>
    ''' <param name="msg"></param>
    Private Shared Sub Add(type As LogType, msg As String)
        Try
            msg = msg.Replace(vbCrLf, " | ").Replace(vbCr, " | ").Replace(vbLf, " | ")
            Debug.WriteLine("[" & type.ToString & "] " & msg)
            Console.WriteLine("[" & Date.Now.ToString("yyyy-MM-dd HH:mm:ss") & "][" & type.ToString & "] " & msg)
            QueueOfLogs.Enqueue(New Log() With {.Type = type, .Message = msg})
        Catch ex As Exception
            'nothing to do
        End Try
    End Sub

    ''' <summary>
    ''' Log an error message
    ''' </summary>
    ''' <param name="msg"></param>
    Public Shared Sub E(msg As String)
        Dim fct As String = New StackFrame(1, True).GetMethod().Name
        Call Add(LogType.ERROR, "[" & fct & "] " & msg)
    End Sub

    ''' <summary>
    ''' Log an information message
    ''' </summary>
    ''' <param name="msg"></param>
    Public Shared Sub I(msg As String)
        Call Add(LogType.INFO, msg)
    End Sub

    ''' <summary>
    ''' Log an debug message
    ''' </summary>
    ''' <param name="msg"></param>
    Public Shared Sub D(msg As String)
        Call Add(LogType.DEBUG, msg)
    End Sub

    ''' <summary>
    ''' Log an warn message
    ''' </summary>
    ''' <param name="msg"></param>
    Public Shared Sub W(msg As String)
        Call Add(LogType.WARN, msg)
    End Sub

    ''' <summary>
    ''' Force flush data into file
    ''' </summary>
    Public Shared Sub ForceFlush()
        If Monitor.TryEnter(MonitorObj) Then
            Try
                Using sw As New StreamWriter(LogFile, True)
                    Dim l As Log = Nothing
                    While QueueOfLogs.TryDequeue(l) = True
                        sw.WriteLine(ToLine(l))
                    End While
                End Using
            Catch ex As Exception
                'nothing to do
            End Try
            Monitor.Exit(MonitorObj)
        End If
    End Sub


End Class
