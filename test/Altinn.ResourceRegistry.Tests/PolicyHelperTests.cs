using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using System.Xml;

namespace Altinn.ResourceRegistry.Tests;

public class PolicyHelperTests
{
    [Fact]
    public void EnsureValidPolicy_StandardResource_WithResourceRegistryAttribute_ShouldSucceed()
    {
        // Arrange
        var resource = new ServiceResource
        {
            Identifier = "altinn_access_management"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">ADMAI</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">altinn_access_management</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:resource"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert
        PolicyHelper.EnsureValidPolicy(resource, policy);
    }

    [Fact]
    public void EnsureValidPolicy_AppResource_WithResourceRegistryAttribute_ShouldSucceed()
    {
        // Arrange
        var resource = new ServiceResource
        {
            Identifier = "app_brg_rrh-innrapportering"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">DAGL</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">app_brg_rrh-innrapportering</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:resource"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert
        PolicyHelper.EnsureValidPolicy(resource, policy);
    }

    [Fact]
    public void EnsureValidPolicy_AppResource_WithOrgAndAppAttributes_ShouldSucceed()
    {
        // Arrange
        var resource = new ServiceResource
        {
            Identifier = "app_brg_rrh-innrapportering"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">DAGL</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">brg</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:org"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">rrh-innrapportering</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:app"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert - should not throw
        PolicyHelper.EnsureValidPolicy(resource, policy);
    }

    [Fact]
    public void EnsureValidPolicy_AppResource_WithWrongOrg_ShouldThrow()
    {
        // Arrange
        var resource = new ServiceResource
        {
            Identifier = "app_brg_rrh-innrapportering"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">DAGL</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">wrongorg</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:org"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">rrh-innrapportering</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:app"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => PolicyHelper.EnsureValidPolicy(resource, policy));
        Assert.Contains("without reference to registry resource id", exception.Message);
    }

    [Fact]
    public void EnsureValidPolicy_AppResource_WithWrongApp_ShouldThrow()
    {
        // Arrange
        var resource = new ServiceResource
        {
            Identifier = "app_brg_rrh-innrapportering"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">DAGL</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">brg</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:org"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">wrongapp</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:app"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => PolicyHelper.EnsureValidPolicy(resource, policy));
        Assert.Contains("without reference to registry resource id", exception.Message);
    }

    [Fact]
    public void EnsureValidPolicy_NonAppResource_WithOrgAndAppAttributes_ShouldThrow()
    {
        // Arrange - non-app resource should not accept org/app attributes
        var resource = new ServiceResource
        {
            Identifier = "some_other_resource"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">DAGL</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">brg</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:org"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">someapp</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:app"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => PolicyHelper.EnsureValidPolicy(resource, policy));
        Assert.Contains("without reference to registry resource id", exception.Message);
    }

    [Fact]
    public void EnsureValidPolicy_AppResource_CaseInsensitive_ShouldSucceed()
    {
        // Arrange - test case insensitive matching
        var resource = new ServiceResource
        {
            Identifier = "app_brg_rrh-innrapportering"
        };

        string policyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xacml:Policy xmlns:xsl=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xacml=""urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"" PolicyId=""urn:altinn:example:policyid:1"" Version=""1.0"" RuleCombiningAlgId=""urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"">
  <xacml:Target/>
  <xacml:Rule RuleId=""urn:altinn:example:ruleid:1"" Effect=""Permit"">
    <xacml:Target>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">DAGL</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:rolecode"" Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">BRG</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:org"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">RRH-INNRAPPORTERING</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:altinn:app"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
      <xacml:AnyOf>
        <xacml:AllOf>
          <xacml:Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
            <xacml:AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</xacml:AttributeValue>
            <xacml:AttributeDesignator AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""false""/>
          </xacml:Match>
        </xacml:AllOf>
      </xacml:AnyOf>
    </xacml:Target>
  </xacml:Rule>
</xacml:Policy>";

        XacmlPolicy policy = ParsePolicy(policyXml);

        // Act & Assert - should succeed with case insensitive comparison
        PolicyHelper.EnsureValidPolicy(resource, policy);
    }

    private static XacmlPolicy ParsePolicy(string xml)
    {
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader);
        return XacmlParser.ParseXacmlPolicy(xmlReader);
    }
}
