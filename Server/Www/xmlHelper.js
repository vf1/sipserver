
var xmlHelper = new function () {

    var reComment = /<!--[\s\S]*?-->/g;

    var removeComments = function (xml) {
        return xml.replace(reComment, '{C097EBC1-2006-4203-A96F-37F7075DC344}');
    }

    var restoreComments = function (xml, originalXml) {

        var comments = originalXml.match(reComment);

        if (comments != null) {
            for (var i = 0; i < comments.length; i++) {
                xml = xml.replace(/{C097EBC1-2006-4203-A96F-37F7075DC344}/, comments[i]);
            }
        }
        return xml;
    }

    this.changeAttribute = function (originalXml, element, attribute, newValue) {

        var xml = removeComments(originalXml);

        var attrAndValue = attribute + '="' + newValue + '"';

        var reAttr = new RegExp(attribute + '=".*?"', 'g');

        var newXml = '';
        if (xml.match(reAttr) != null) {
            newXml = xml.replace(reAttr, attrAndValue);
        }
        else {
            var index = xml.indexOf(element);
            if (index >= 0) {
                index += element.length;
                newXml = xml.slice(0, index) + '\n\t' + attrAndValue + xml.slice(index)
            }
        }

        return restoreComments(newXml, originalXml)
    }
}
