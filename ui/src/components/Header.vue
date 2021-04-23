<template>
	<v-app-bar
		app
		color="primary">
		<v-toolbar-title>Wishare</v-toolbar-title>

		<v-spacer/>

		<v-btn 
			rounded
			text
			class="mx-1"
			to="/login">Sign in</v-btn>
		<v-btn
			rounded
			class="mx-1"
			to="/signup">Sign up</v-btn>
	</v-app-bar>
</template>

<script lang="ts">

import { Vue, Component } from "vue-property-decorator"

@Component
export default class Header extends Vue {
}

</script>