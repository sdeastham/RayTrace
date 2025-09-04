namespace RayTrace;

public abstract class PDF
{
    public virtual double Value(Vector3d direction)
    {
        return 0.0;
    }

    public virtual Vector3d Generate(RTRandom generator)
    {
        return new Vector3d(1.0, 0.0, 0.0);
    }
}

public class SpherePDF : PDF
{
    public override double Value(Vector3d direction)
    {
        return 1.0 / (4.0 * Math.PI);
    }

    public override Vector3d Generate(RTRandom generator)
    {
        return generator.RandomUnitVector();
    }
}

public class CosinePDF : PDF
{
    private OrthoNormalBasis ONB;

    public CosinePDF(Vector3d w)
    {
        ONB = new OrthoNormalBasis(w);
    }

    public override double Value(Vector3d direction)
    {
        double cosine = Vector3d.Dot(direction.UnitVector, ONB.W);
        return cosine <= 0.0 ? 0.0 : cosine / Math.PI;
    }

    public override Vector3d Generate(RTRandom generator)
    {
        var dir = generator.RandomCosineDirection();
        return ONB.Transform(dir);
    }
}

public class HittablePDF(IHittable objects, Vector3d origin) : PDF
{
    private Vector3d Origin = origin;
    private IHittable Objects = objects;

    public override double Value(Vector3d direction)
    {
        return Objects.PDFValue(Origin, direction);
    }

    public override Vector3d Generate(RTRandom generator)
    {
        return Objects.Random(Origin, generator);
    }
}

public class MixturePDF : PDF
{
    private PDF[] PDFs = new PDF[2];
    public MixturePDF(PDF p0, PDF p1)
    {
        PDFs[0] = p0;
        PDFs[1] = p1;
    }

    public override double Value(Vector3d direction)
    {
        return 0.5 * PDFs[0].Value(direction) + 0.5 * PDFs[1].Value(direction);
    }

    public override Vector3d Generate(RTRandom generator)
    {
        if (generator.RandomDouble() < 0.5)
            return PDFs[0].Generate(generator);
        else
            return PDFs[1].Generate(generator);
    }
}