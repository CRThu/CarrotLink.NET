import clr

clr.AddReference('System')
from System import DateTime  # type: ignore

clr.AddReference('System.Windows.Forms')
from System.Windows.Forms import MessageBox  # type: ignore

if __name__ == '__main__':
    print('hello')
    print(dir(clr))
    print(DateTime.Now)
    MessageBox.Show("Hello from pythonnet")
