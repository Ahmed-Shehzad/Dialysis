using Dialysis.Prescription.Application.Domain.Hl7;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class MdcToObxSubIdCatalogTests
{
    [Fact]
    public void Get_KnownCode_ReturnsMappedSubId()
    {
        MdcToObxSubIdCatalog.Get("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING").ShouldBe("1.1.3.1");
        MdcToObxSubIdCatalog.Get("MDC_HDIALY_UF_RATE_SETTING").ShouldBe("1.1.9.2");
        MdcToObxSubIdCatalog.Get("MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE").ShouldBe("1.1.9.4");
        MdcToObxSubIdCatalog.Get("MDC_HDIALY_MACH_TIME").ShouldBe("1.1.1.1");
    }

    [Fact]
    public void Get_UnknownCode_ReturnsNull()
    {
        MdcToObxSubIdCatalog.Get("UNKNOWN_CODE").ShouldBeNull();
        MdcToObxSubIdCatalog.Get("").ShouldBeNull();
        MdcToObxSubIdCatalog.Get(null!).ShouldBeNull();
    }

    [Fact]
    public void Get_CaseInsensitive_ReturnsMappedSubId()
    {
        MdcToObxSubIdCatalog.Get("mdc_hdialy_uf_rate_setting").ShouldBe("1.1.9.2");
        MdcToObxSubIdCatalog.Get("MDC_HDIALY_UF_RATE_SETTING").ShouldBe("1.1.9.2");
    }

    [Fact]
    public void GetOrDefault_KnownCode_ReturnsMappedSubId()
    {
        MdcToObxSubIdCatalog.GetOrDefault("MDC_HDIALY_UF_MODE").ShouldBe("1.1.9.1");
    }

    [Fact]
    public void GetOrDefault_UnknownCode_ReturnsDefault()
    {
        MdcToObxSubIdCatalog.GetOrDefault("UNKNOWN").ShouldBe("1.1.9.1");
        MdcToObxSubIdCatalog.GetOrDefault("UNKNOWN", "9.9.9.9").ShouldBe("9.9.9.9");
    }
}
