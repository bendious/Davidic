mergeInto(LibraryManager.library, {
	UpdateInternal: function (element_id, osmd_params, xml_str) {
		// create OSMD instance if necessary
		var id_str = Pointer_stringify(element_id);
		if (document.osmds == null) {
			document.osmds = {};
		}
		if (!(id_str in document.osmds)) {
			document.osmds[id_str] = new opensheetmusicdisplay.OpenSheetMusicDisplay(id_str, { drawingParameters: Pointer_stringify(osmd_params) });
		}

		// output
		xml_str = Pointer_stringify(xml_str);
		console.log(xml_str); // TEMP?
		document.osmds[id_str].load(xml_str).then(function() {
			document.osmds[id_str].render();
		});
	},
});
