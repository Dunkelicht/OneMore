﻿//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
	using System.Linq;
	using System.Text;
    using System.Xml;
    using System.Xml.Linq;


	internal class ApplyStyleCommand : Command
	{

		private XElement page;
		private XNamespace ns;
		private Stylizer stylizer;
		private Style style;


		public ApplyStyleCommand() : base()
		{
		}


		public void Execute(int selectedIndex)
		{
			using (var manager = new ApplicationManager())
			{
				page = manager.CurrentPage();
				if (page != null)
				{
					ns = page.GetNamespaceOfPrefix("one");
					style = new StyleProvider().GetStyle(selectedIndex);
					stylizer = new Stylizer(style);

					bool success = style.StyleType == StyleType.Character
						? StylizeWords()
						: StylizeParagraphs();

					if (success)
					{
						manager.UpdatePageContent(page);
					}
				}
			}
		}


		private bool StylizeWords()
		{
			// find all selected T element; may be multiple if text is selected across 
			// multiple paragraphs - OEs - but partial paragraphs may be selected too...

			var selections = page.Descendants(ns + "T")
				.Where(e => e.Attributes("selected").Any(a => a.Value.Equals("all")));

			if (selections == null)
			{
				// shouldn't happen, but...
				return false;
			}

			foreach (var selection in selections)
			{
				bool empty = true;

				if (selection.Parent.Nodes().Count() == 1)
				{
					// OE parent must have only this T

					stylizer.ApplyStyle(selection);
				}
				else
				{
					// OE parent has multiple Ts so test if we need to merge them

					//logger.WriteLine("selection.parent:" + (selection.Parent as XElement).ToString(SaveOptions.None));

					var cdata = selection.GetCData();

					// text cursor is positioned but selection length is 0
					if (cdata.IsEmpty())
					{
						// inside a word, adjacent to a word, or somewhere in whitespace?

						var prev = selection.PreviousNode as XElement;
						if ((prev != null) && prev.GetCData().EndsWithWhitespace())
						{
							prev = null;
						}

						var next = selection.NextNode as XElement;
						if ((next != null) && next.GetCData().StartsWithWhitespace())
						{
							next = null;
						}

						if ((prev != null) && (next != null))
						{
							empty = false;

							// navigate to closest word

							var word = new StringBuilder();

							if (prev != null)
							{
								logger.WriteLine("prev:" + prev.ToString(SaveOptions.None));

								if (!prev.GetCData().EndsWithWhitespace())
								{
									// grab previous part of word
									word.Append(prev.ExtractLastWord());
									//logger.WriteLine("word with prev:" + word.ToString());
									//logger.WriteLine("prev updated:" + prev.ToString(SaveOptions.None));
									//logger.WriteLine("parent:" + (selection.Parent as XElement).ToString(SaveOptions.None));

									if (prev.GetCData().Value.Length == 0)
									{
										prev.Remove();
									}
								}
							}

							if (next != null)
							{
								logger.WriteLine("next:" + next.ToString(SaveOptions.None));

								if (!next.GetCData().StartsWithWhitespace())
								{
									// grab following part of word
									word.Append(next.ExtractFirstWord());
									//logger.WriteLine("word with next:" + word.ToString());
									//logger.WriteLine("next updated:" + next.ToString(SaveOptions.None));
									//logger.WriteLine("parent:" + (selection.Parent as XElement).ToString(SaveOptions.None));

									if (next.GetCData().Value.Length == 0)
									{
										next.Remove();
									}
								}
							}

							if (word.Length > 0)
							{
								selection.DescendantNodes().OfType<XCData>()
									.First()
									.ReplaceWith(new XCData(word.ToString()));

								//logger.WriteLine("parent udpated:" + (selection.Parent as XElement).ToString(SaveOptions.None));
							}
							else
							{
								empty = true;
							}

							//logger.WriteLine("parent:" + (selection.Parent as XElement).ToString(SaveOptions.None));
						}
					} 

					if (empty)
					{
						stylizer.ApplyStyle(selection.GetCData());
						//logger.WriteLine("final empty parent:" + (selection.Parent as XElement).ToString(SaveOptions.None));
					}
					else
					{
						stylizer.ApplyStyle(selection);
						//logger.WriteLine("final parent:" + (selection.Parent as XElement).ToString(SaveOptions.None));
					}
				}
			}

			return true;
		}


		private bool StylizeParagraphs()
		{
			// find all paragraphs - OE elements - that have selections
			var elements = page.Descendants()
				.Where(p => p.NodeType == XmlNodeType.Element
					&& p.Name.LocalName == "T"
					&& p.Attributes("selected").Any(a => a.Value.Equals("all")))
				.Select(p => p.Parent);

			if (elements?.Any() == true)
			{
				var css = style.ToCss();

				var applied = new Style(style)
				{
					ApplyColors = true
				};

				System.Diagnostics.Debugger.Launch();

				foreach (var element in elements)
				{
					// clear any existing style on or within the paragraph
					// note that apply-colors translates to clear-colors within the method
					stylizer.Clear(element, style.ApplyColors);

					// style may still exist if apply colors if false and there are colors
					var attr = element.Attribute("style");
					if (attr == null)
					{
						// blast style onto paragraph, let OneNote normalize across
						// children if it wants
						attr = new XAttribute("style", css);
						element.Add(attr);
					}
					else
					{
						applied.MergeColors(new Style(attr.Value));
						attr.Value = applied.ToCss();
					}

					ApplySpacing(element, "spaceBefore", style.SpaceBefore);
					ApplySpacing(element, "spaceAfter", style.SpaceAfter);
				}

				return true;
			}

			return false;
		}


		private void ApplySpacing(XElement element, string name, string space)
		{
			var attr = element.Attribute(name);
			if (attr == null)
			{
				element.Add(new XAttribute(name, space));
			}
			else
			{
				attr.Value = space;
			}
		}
	}

	/*
	 * one:OE -------------------------
	 *
	 * T=all but OE=partial because EOL is not selected - NOTE ONE CHILD
	  <one:OE creationTime="2020-03-15T23:29:18.000Z" lastModifiedTime="2020-03-15T23:29:18.000Z" objectID="{BF7825D6-1EE4-46C0-AC87-B2FFA76137D1}{15}{B0}" alignment="left" quickStyleIndex="1" selected="partial">
        <one:T selected="all"><![CDATA[This is the fourth line]]></one:T>
      </one:OE>
	 *
	 * T=all and OE=all because EOL is selected - NOTE ONE CHILD
	  <one:OE creationTime="2020-03-15T23:29:18.000Z" lastModifiedTime="2020-03-15T23:29:18.000Z" objectID="{BF7825D6-1EE4-46C0-AC87-B2FFA76137D1}{15}{B0}" selected="all" alignment="left" quickStyleIndex="1">
        <one:T selected="all"><![CDATA[This is the fourth line]]></one:T>
      </one:OE>
	 * 
	 * one:T --------------------------
	 * 
	 * middle of word
      <one:OE creationTime="2020-03-15T23:29:18.000Z" lastModifiedTime="2020-03-15T23:29:18.000Z" objectID="{BF7825D6-1EE4-46C0-AC87-B2FFA76137D1}{15}{B0}" alignment="left" quickStyleIndex="1" selected="partial">
        <one:T><![CDATA[This is the fo]]></one:T>
        <one:T selected="all"><![CDATA[]]></one:T>
        <one:T><![CDATA[urth line]]></one:T>
      </one:OE>
	 * 
	 * selected word
      <one:OE creationTime="2020-03-15T23:29:18.000Z" lastModifiedTime="2020-03-15T23:29:18.000Z" objectID="{BF7825D6-1EE4-46C0-AC87-B2FFA76137D1}{15}{B0}" alignment="left" quickStyleIndex="1" selected="partial">
        <one:T><![CDATA[This is the ]]></one:T>
        <one:T selected="all"><![CDATA[fourth ]]></one:T>
        <one:T><![CDATA[line]]></one:T>
      </one:OE>
	 *
 	 */
}