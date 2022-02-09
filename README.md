# NXIngest

NXIngest extracts metadata from a nexus file into a format that is appropriate for ingestion into an ICAT instance. It outputs an xml file in a format defined by a mapping file, which should conform to the icatXSD schema.

## Usage


## Structure



## Mapping file

NXIngest must be given a mapping file to define the structure of the output. Mapping files are XML and contain 4 types of elements, each of which becomes one or more elements in the output. The types of element allowed in the mapping file are:

 - Elements with a `type` attribute with the value 'tbl': Become elements of the same name with child elements that match the child elements of th element in the mapping file.
 - `record` elements: Become elements whose tag is equal to the value of the `record`s `icat_name` child, and whose value is equal to the resolved value of its `value` child (see [Mapping value types](#mapping-value-types) for how values are resolved). Both children are required, and the `value` child must have a type attribute.
 - `parameter` elements: Become `parameter` elements with children that are mapped from the children in the mapping file:
   - The (required) `icat_name` child in the mapping file becomes a `name` element
   - The (required) `value` child in the mapping file becomes a `numeric_value` or `string_value` element, based on whether the `parameter` has a type attribute of 'param_str' or 'param_num'. The value of the `numeric/string_value` in the output is determined in the same way as the `record` elements `value`.
   - The (optional) `units` child in the mapping file becomes a `units` element whose value is determined in the same way as the `value`s.
   - The (optional) `description` child in the mapping file becomes a `description` element whose value is equal to the value in the mapping file.
 - `keyword` elements: Become several `keyword` elements. The `value` child in the mapping file is resolved in the same way as other `value` elements, then each word of the output becomes a `keyword` element with a `name` child equal to the word. Some extra processing is done to remove ie. common words or bad punctuation.

The mapping file must contain an `icat` element with a 'type="tbl"' attribute, which becomes the root element of the output.

### Mapping value types

Many elements in the mapping file include a value which is resolved into the value of a corresponding element in the output file. The way the output value is resolved from the mapping value depends on the elements 'type' attribute. There are 4 valid values for the 'type' attribute:

 - `fix`: The value is used as it is in the mapping file, with whitespace trimmed.
 - `nexus`: The value is treated as a path into the nexus file being ingested. The path must point at either a dataset or attribute in the file; if the path contains a '.', it is assumed to be an attribute, otherwise it is assumed to be a dataset. The value must be either a string, integer, or floating point number.
   - If the value ends with "[f]", where 'f' is one of "SUM", "AVG", "MAX", "MIN", or "STD", the value is instead treated as an array of unsigned integers, and aggregated using the matching function.
 - `special`: The value must start with either:
   - `time:`: The rest of the value must be either:
     - `now`: The current date time is used.
     - `nexus(/path)`: A value is looked up from the nexus file being ingested, and parsed as a DateTime.
     - Optionally, if there is a semicolon in the value the right side is treated as a format string for the date. Format strings must use the [strftime](https://www.cplusplus.com/reference/ctime/strftime/) syntax.
   - `sys`: The rest of the value is one of:
     - `size`: The size of the nexus file being ingested is used.
     - `location`: The full path to the nexus file being ingested is used.
     - `filename`: The name of the nexus file being ingested is used.
 - `mix`: The value is split on '|' characters, then each part is split on a ':'; the left side must be a mapping value type (not `mix`), and the right side is a value resolved in the appropriate way.
