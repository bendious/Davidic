mergeInto(LibraryManager.library, {
	OSMD_update: function () {
		// TODO: re-use existing OSMD instance, pull in actual notes from C#
		var osmd = new opensheetmusicdisplay.OpenSheetMusicDisplay("osmd", { drawingParameters: "compacttight" });
		var loadPromise = osmd.load("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\
			<!DOCTYPE score-partwise PUBLIC \"-//Recordare//DTD MusicXML 2.0 Partwise//EN\"\
			  \"http://www.musicxml.org/dtds/partwise.dtd\">\
			<score-partwise version=\"2.0\">\
			  <defaults>\
				<scaling>\
				  <millimeters>2.1166666679999997</millimeters>\
				  <tenths>10</tenths>\
				</scaling>\
				<page-layout>\
				  <page-height>1320</page-height>\
				  <page-width>1020</page-width>\
				  <page-margins type=\"both\">\
					<left-margin>59.583333333333336</left-margin>\
					<right-margin>56.66666666666667</right-margin>\
					<top-margin>96.66666666666667</top-margin>\
					<bottom-margin>80</bottom-margin>\
				  </page-margins>\
				</page-layout>\
				<system-layout>\
				  <system-margins>\
					<left-margin>59.583333333333336</left-margin>\
					<right-margin>26.666666666666668</right-margin>\
				  </system-margins>\
				  <system-distance>92.5</system-distance>\
				  <top-system-distance>145.83333333333334</top-system-distance>\
				</system-layout>\
				<staff-layout>\
				  <staff-distance>55</staff-distance>\
				</staff-layout>\
			  </defaults>\
			  <part-list>\
				<score-part id=\"P1\">\
				  <part-name>Piano (right)</part-name>\
				  <part-abbreviation>Piano (right)</part-abbreviation>\
				  <score-instrument id=\"P1I1\">\
					<instrument-name>Piano (right)</instrument-name>\
				  </score-instrument>\
				  <midi-instrument id=\"P1I1\">\
					<midi-channel>1</midi-channel>\
					<midi-program>1</midi-program>\
				  </midi-instrument>\
				</score-part>\
			  </part-list>\
			  <part id=\"P1\">\
				<measure number=\"1\">\
				  <attributes>\
					<divisions>64</divisions>\
					<key>\
					  <fifths>0</fifths>\
					  <mode>major</mode>\
					</key>\
					<time>\
					  <beats>4</beats>\
					  <beat-type>4</beat-type>\
					</time>\
					<clef number=\"1\">\
					  <sign>G</sign>\
					  <line>2</line>\
					</clef>\
				  </attributes>\
				  <direction placement=\"above\">\
					<direction-type>\
					  <metronome>\
						<beat-unit>quarter</beat-unit>\
						<per-minute>116</per-minute>\
					  </metronome>\
					</direction-type>\
					<sound tempo=\"116\"/>\
				  </direction>\
				  <direction placement=\"above\">\
					<direction-type>\
					  <metronome>\
						<beat-unit>quarter</beat-unit>\
						<per-minute>116</per-minute>\
					  </metronome>\
					</direction-type>\
					<sound tempo=\"116\"/>\
				  </direction>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>96</duration>\
					<voice>1</voice>\
					<type>quarter</type>\
					<dot/>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>E</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>C</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>4</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>4</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>4</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">end</beam>\
				  </note>\
				</measure>\
				<measure number=\"2\">\
				  <attributes/>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>96</duration>\
					<voice>1</voice>\
					<type>quarter</type>\
					<dot/>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>E</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>C</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>D</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>E</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>F</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">end</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>B</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>C</step>\
					  <octave>6</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">end</beam>\
				  </note>\
				</measure>\
				<measure number=\"3\">\
				  <attributes/>\
				  <note>\
					<pitch>\
					  <step>B</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">end</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>B</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <alter>-1</alter>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<accidental>flat</accidental>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>16</duration>\
					<voice>1</voice>\
					<type>16th</type>\
					<accidental>natural</accidental>\
					<beam number=\"1\">end</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>C</step>\
					  <octave>6</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>A</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">end</beam>\
				  </note>\
				</measure>\
				<measure number=\"4\">\
				  <attributes/>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <alter>-1</alter>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<accidental>flat</accidental>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<accidental>natural</accidental>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">end</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>G</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>64</duration>\
					<voice>1</voice>\
					<type>quarter</type>\
				  </note>\
				  <note>\
					<rest/>\
					<duration>64</duration>\
					<voice>1</voice>\
					<type>quarter</type>\
				  </note>\
				</measure>\
				<measure number=\"5\">\
				  <attributes/>\
				  <direction placement=\"below\">\
					<direction-type>\
					  <dynamics placement=\"below\">\
						<p/>\
					  </dynamics>\
					</direction-type>\
				  </direction>\
				  <note>\
					<pitch>\
					  <step>D</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<chord/>\
					<pitch>\
					  <step>F</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">begin</beam>\
				  </note>\
				  <note>\
					<pitch>\
					  <step>D</step>\
					  <octave>5</octave>\
					</pitch>\
					<duration>32</duration>\
					<voice>1</voice>\
					<type>eighth</type>\
					<beam number=\"1\">continue</beam>\
				  </note>\
				  <barline location=\"right\">\
					<bar-style>light-heavy</bar-style>\
				  </barline>\
				</measure>\
			  </part>\
			</score-partwise>");
		loadPromise.then(function(){
			osmd.render();
		});
	},
});