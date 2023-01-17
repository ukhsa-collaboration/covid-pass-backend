namespace CovidCertificate.Backend.Interfaces
{
    public interface IMapper<TSource, TTarget>
    {
        TTarget MapAsync(TSource original);
    }
}
