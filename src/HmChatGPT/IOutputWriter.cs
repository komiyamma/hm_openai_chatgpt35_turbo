interface IOutputWriter
{
    string Normalize(string? msg);

    int Write(string msg);

    int WriteLine(string msg);
}
