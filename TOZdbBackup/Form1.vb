Imports System.Reflection
Imports System.Data

Imports MySql.Data
Imports MySql.Data.MySqlClient
Imports System.IO

Public Class Form1
    'Dim runTimeResourceSet As Object
    'Dim dictEntry As DictionaryEntry
    Dim HostDisplaysource As DataTable
    'Dim cs As String = "Database=Bix360;Data Source=43.204.71.137;" & "User Id=admin;Password=smrtalentoz3106"
    Dim cs As String = "Database=Bix360;Data Source=talentoz-rds-dev-test.cenybq37vou2.ap-south-1.rds.amazonaws.com;" & "User Id=admin;Password=WB6jwd9VL9Uh3ZDb"
    Dim newdbname = ""
    Dim processLoop = 0
    Private WithEvents MyProcess As Process
    Private Delegate Sub AppendOutputTextDelegate(ByVal text As String)
    'Public conn As MySqlConnection()
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Label4.Visible = False
        'runTimeResourceSet = My.Resources.ResourceManager.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, True, False)
        'Dim cs As String = "Database=Bix360;Data Source=43.204.71.137;" & "User Id=admin;Password=3%C^9co*TTteBu75"
        Try
            Dim conn = New MySql.Data.MySqlClient.MySqlConnection(cs)
            conn.Open()
            Dim myCommand As New MySqlCommand("select * from TOZ_backup_sys_connector group by HostName", conn)
            Dim da As MySqlDataAdapter = New MySqlDataAdapter(myCommand)
            Dim dt As New DataTable("HostName_source")
            da.Fill(dt)
            HostDisplaysource = dt
            If dt.Rows.Count > 0 Then
                ComboBox1.DataSource = dt
                ComboBox1.DisplayMember = "HostDisplayName"
                ComboBox1.ValueMember = "HostDisplayName"
            End If
            conn.Close()
        Catch ex As Exception
        End Try
        ComboBox1.Text = "Select RDS from..."

        'ComboBox1.Items.Clear()
        'For Each dictEntry In runTimeResourceSet
        '    ComboBox1.Items.Add(dictEntry.Key)
        'Next
        DateTimePicker1.MinDate = DateTime.Now.AddDays(-30)
        DateTimePicker1.MaxDate = DateTime.Now.AddDays(-1)

        Me.AcceptButton = Button1
        MyProcess = New Process
        With MyProcess.StartInfo
            .FileName = "CMD.EXE"
            .UseShellExecute = False
            .CreateNoWindow = True
            .RedirectStandardInput = True
            .RedirectStandardOutput = True
            .RedirectStandardError = True
        End With
        MyProcess.Start()

        MyProcess.BeginErrorReadLine()
        MyProcess.BeginOutputReadLine()
        AppendOutputText("Process Started at: " & MyProcess.StartTime.ToString)
    End Sub

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        MyProcess.StandardInput.WriteLine("EXIT") 'send an EXIT command to the Command Prompt
        MyProcess.StandardInput.Flush()
        MyProcess.Close()
    End Sub

    Private Sub MyProcess_ErrorDataReceived(ByVal sender As Object, ByVal e As System.Diagnostics.DataReceivedEventArgs) Handles MyProcess.ErrorDataReceived
        AppendOutputText(vbCrLf & "Error: " & e.Data)
    End Sub

    Private Sub MyProcess_OutputDataReceived(ByVal sender As Object, ByVal e As System.Diagnostics.DataReceivedEventArgs) Handles MyProcess.OutputDataReceived
        AppendOutputText(vbCrLf & e.Data)
    End Sub

    Private Sub MyProcess_HasExited() Handles MyProcess.Exited
        MessageBox.Show("Done")
    End Sub

    Private Sub AppendOutputText(ByVal text As String)

        If OutputTextBox.InvokeRequired Then
            Dim myDelegate As New AppendOutputTextDelegate(AddressOf AppendOutputText)
            Me.Invoke(myDelegate, text)
        Else
            OutputTextBox.AppendText(text)
        End If
        If text.Contains("Email and password has been changed") Then
            If processLoop = 0 Then
                processLoop += 1
                MessageBox.Show("Database restored with the name as " & newdbname)
            End If
        End If

    End Sub
    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        'For Each dictEntry In runTimeResourceSet
        'If dictEntry.Key = ComboBox1.Text Then
        'Dim mCrypTo As New SMRHRT.Services.Security.CryptoProvider("smr357951hrdpower312net")
        'Dim str = mCrypTo.DecryptString(dictEntry.Value)
        If ComboBox1.Text.ToString() <> "System.Data.DataRowView" Then
            Try
                Dim conn = New MySql.Data.MySqlClient.MySqlConnection(cs)
                conn.Open()
                Dim myCommand As New MySqlCommand("SELECT * FROM TOZ_backup_sys_connector where HostDisplayName='" & ComboBox1.Text & "'", conn)
                Dim da As MySqlDataAdapter = New MySqlDataAdapter(myCommand)
                Dim dt As New DataTable("Client_details")
                da.Fill(dt)
                If dt.Rows.Count > 0 Then
                    ComboBox2.DataSource = dt
                    ComboBox2.DisplayMember = "connectorName"
                    ComboBox2.ValueMember = "connectorName"
                End If
                ComboBox2.Text = "Select DB from..."
                conn.Close()
            Catch ex As Exception
            End Try
        End If

        ' End If
        'Next
    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If SaveValidate() Then
            Button1.Enabled = False
            Label5.Visible = True
            Dim filename = ""
            Dim serverName = ""
            For Each row As DataRow In HostDisplaysource.Rows
                If row("HostDisplayName") = ComboBox1.Text Then
                    serverName = row("HostName")
                End If
            Next

            filename = serverName & "/" & DateTimePicker1.Text & "/" & ComboBox2.Text & DateTimePicker1.Text & ".sql.gz"
            Dim source = "s3://s3-talentoz-dev/"
            Dim host = "talentoz-rds-dev-test.cenybq37vou2.ap-south-1.rds.amazonaws.com"
            Dim target = "C:\Users\Administrator\Desktop\DB_Backup_Restoration\"
            newdbname = ComboBox2.Text.Split("_")(0) & "_" & CDate(DateTimePicker1.Text).Day & MonthName(CDate(DateTimePicker1.Text).Month, True) & CDate(DateTimePicker1.Text).Year

            MyProcess.StandardInput.WriteLine("aws s3 cp " & source & filename & " " & target)
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("cd " & target)
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("""C:\Program Files\7-Zip\7z.exe""" & " e " & """" & target & ComboBox2.Text & DateTimePicker1.Text & ".sql.gz""" & " -o*")
            MyProcess.StandardInput.Flush()
            'System.Threading.Thread.Sleep(60000)

            'Dim text As String = File.ReadAllText(target & ComboBox2.Text & DateTimePicker1.Text & ".sql\" & ComboBox2.Text & DateTimePicker1.Text & ".sql")
            'text = text.Replace("DEFINER=`admin`@`%`", "DEFINER=`admin`@`%`")
            'File.WriteAllText(target & ComboBox2.Text & DateTimePicker1.Text & ".sql\" & ComboBox2.Text & DateTimePicker1.Text & ".sql", text)

            'MyProcess.StandardInput.WriteLine("cscript.exe C:\DB_patch_restoration\replace.vbs """ & target & ComboBox2.Text & DateTimePicker1.Text & ".sql\" & ComboBox2.Text & DateTimePicker1.Text & ".sql""" & " ""DEFINER=`admin`@`%`""" & " ""DEFINER=`adminc`@`%`""")
            'MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("""C:\Program Files\MySQL\MySQL Workbench 8.0 CE\mysql.exe""" & " -h " & host & " --user=admin --password=WB6jwd9VL9Uh3ZDb -s -N -e " & """create database " & newdbname & ";""")
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("cd C:\Program Files\MySQL\MySQL Server 8.0 CE\")
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("""C:\Program Files\MySQL\MySQL Workbench 8.0 CE\mysql.exe""" & " -h " & host & " --user=admin --password=WB6jwd9VL9Uh3ZDb " & newdbname & "< " & target & ComboBox2.Text & DateTimePicker1.Text & ".sql\" & ComboBox2.Text & DateTimePicker1.Text & ".sql""")
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("echo Database restored with the name as :  " & newdbname)
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("@RD /S /Q " & """" & target & ComboBox2.Text & DateTimePicker1.Text & ".sql""")
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("del " & target & ComboBox2.Text & DateTimePicker1.Text & ".sql.gz")
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("""C:\Program Files\MySQL\MySQL Workbench 8.0 CE\mysql.exe""" & " -h " & host & " --user=admin --password=WB6jwd9VL9Uh3ZDb " & newdbname & "< " & "C:\DB_patch_restoration\restore_removal_script.sql""")
            MyProcess.StandardInput.Flush()

            MyProcess.StandardInput.WriteLine("echo Email and password has been changed.")
            MyProcess.StandardInput.Flush()

            saveData()
            'Label4.Text = ""
            'Label4.Visible = True
            'Label4.Text = "Database restored with the name as :  " & newdbname
            'MessageBox.Show("Database restored with the name as :  " & newdbname)
        Else
            MessageBox.Show("Please fill the all required details")
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        MyProcess.StandardInput.WriteLine("""C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe""" & " --user=root --password=admin312 -s -N -e " & """create database test123;""")
        MyProcess.StandardInput.Flush()

        MyProcess.WaitForExit()
    End Sub

    Private Sub saveData()
        Dim conn = New MySql.Data.MySqlClient.MySqlConnection(cs)
        conn.Open()
        Dim sql As String = "insert into toz_backup_sys_connector_audit (DBName,BackupDate,RestoreDBName, DevName, Reason, DueDate) values (@DBName,@BackupDate,@RestoreDBName,@DevName,@Reason,@DueDate)"

        Dim cmd = New MySqlCommand(sql, conn)
        cmd.Parameters.AddWithValue("@DBName", ComboBox2.Text)
        cmd.Parameters.AddWithValue("@RestoreDBName", newdbname)
        cmd.Parameters.AddWithValue("@BackupDate", CDate(DateTimePicker1.Text))
        cmd.Parameters.AddWithValue("@DevName", TextBox1.Text)
        cmd.Parameters.AddWithValue("@Reason", TextBox2.Text)
        cmd.Parameters.AddWithValue("@DueDate", CDate(DateTimePicker2.Text))
        cmd.ExecuteNonQuery()
        conn.Close()
    End Sub

    Private Function SaveValidate() As Boolean
        If (ComboBox2.Text <> "" And TextBox1.Text <> "" And TextBox2.Text <> "") Then
            Return True
        End If
        Return False
    End Function
End Class
